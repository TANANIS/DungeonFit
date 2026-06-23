import argparse
import base64
import json
import mimetypes
import os
import threading
import time
from io import BytesIO
from datetime import datetime
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, unquote, urlparse
import webbrowser

from PIL import Image


DASHBOARD_ROOT = Path(__file__).resolve().parent
PROJECT_ROOT = DASHBOARD_ROOT.parent.parent
MANIFEST_PATH = DASHBOARD_ROOT / "art_manifest.json"
STATE_PATH = DASHBOARD_ROOT / "art_state.local.json"
PID_PATH = DASHBOARD_ROOT / "art_dashboard_server.pid"


def now_text():
    return datetime.now().isoformat(timespec="seconds")


def read_manifest():
    return json.loads(MANIFEST_PATH.read_text(encoding="utf-8-sig"))


def empty_state():
    return {"updatedAt": now_text(), "uploads": {}, "checks": {}}


def read_state():
    if not STATE_PATH.exists():
        return empty_state()
    try:
        state = json.loads(STATE_PATH.read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError:
        return empty_state()
    state.setdefault("updatedAt", now_text())
    state.setdefault("uploads", {})
    state.setdefault("checks", {})
    return state


def save_state(state):
    state["updatedAt"] = now_text()
    STATE_PATH.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")


def write_pid_file(port):
    PID_PATH.write_text(
        json.dumps(
            {
                "pid": os.getpid(),
                "port": port,
                "url": f"http://127.0.0.1:{port}/",
                "startedAt": now_text(),
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )


def remove_pid_file():
    try:
        if PID_PATH.exists():
            PID_PATH.unlink()
    except OSError:
        pass


def find_asset(asset_id):
    for asset in read_manifest()["assets"]:
        if asset["id"] == asset_id:
            return asset
    return None


def safe_asset_path(asset):
    relative_path = asset["relativePath"]
    if ".." in relative_path or not relative_path.endswith(".png") or not relative_path.startswith("Assets/Art/"):
        raise ValueError(f"Unsafe asset path: {relative_path}")
    target = (PROJECT_ROOT / relative_path.replace("/", os.sep)).resolve()
    root = PROJECT_ROOT.resolve()
    if root != target and root not in target.parents:
        raise ValueError(f"Asset path escapes project root: {relative_path}")
    return target


def asset_sample_path(asset):
    sample_path = asset.get("samplePath", "")
    target = (DASHBOARD_ROOT / sample_path.replace("/", os.sep)).resolve()
    root = DASHBOARD_ROOT.resolve()
    if root != target and root not in target.parents:
        raise ValueError(f"Sample path escapes dashboard root: {sample_path}")
    return target


def analyze_png(asset):
    target = safe_asset_path(asset)
    exists = target.exists()
    if not exists:
        return {
            "exists": False,
            "source": "missing",
            "checks": {
                "png_file": {"status": "fail", "message": "Target PNG is missing."},
                "size_correct": {"status": "fail", "message": "Target PNG is missing."},
                "transparent_background": {"status": "fail", "message": "Target PNG is missing." if asset.get("transparent") else "Not required."},
                "clean_edges": {"status": "manual", "message": "Upload final art first."},
                "no_baked_text": {"status": "manual", "message": "Needs visual review."},
                "godot_import_tested": {"status": "fail", "message": "No target PNG to import."},
            },
        }

    result = {
        "exists": True,
        "source": "target",
        "bytes": target.stat().st_size,
        "mtime": target.stat().st_mtime,
        "checks": {},
    }
    try:
        with Image.open(target) as image:
            width, height = image.size
            result["width"] = width
            result["height"] = height
            result["mode"] = image.mode
            result["format"] = image.format
            result["hasAlpha"] = image.mode in ("RGBA", "LA") or "transparency" in image.info
            result["textChunks"] = sorted(image.text.keys()) if hasattr(image, "text") else []

            expected_width = int(asset.get("expectedWidth", 0))
            expected_height = int(asset.get("expectedHeight", 0))
            size_ok = width == expected_width and height == expected_height
            result["checks"]["png_file"] = {"status": "pass", "message": "Readable PNG."}
            result["checks"]["size_correct"] = {
                "status": "pass" if size_ok else "fail",
                "message": f"{width}x{height}, expected {expected_width}x{expected_height}.",
            }

            if asset.get("transparent"):
                alpha_ok = bool(result["hasAlpha"])
                result["checks"]["transparent_background"] = {
                    "status": "pass" if alpha_ok else "fail",
                    "message": "Has alpha/transparency channel." if alpha_ok else "No alpha/transparency channel detected.",
                }
            else:
                result["checks"]["transparent_background"] = {"status": "pass", "message": "Not required for this asset."}

            result["checks"]["clean_edges"] = check_edges(image, asset)
            result["checks"]["no_baked_text"] = {
                "status": "warn" if result["textChunks"] else "manual",
                "message": "PNG metadata contains text chunks." if result["textChunks"] else "No PNG text metadata; visual text still needs review.",
            }
    except Exception as error:
        result["checks"]["png_file"] = {"status": "fail", "message": f"Cannot read PNG: {error}"}

    import_path = Path(str(target) + ".import")
    result["importExists"] = import_path.exists()
    result["checks"]["godot_import_tested"] = {
        "status": "pass" if result["importExists"] else "warn",
        "message": ".import file exists." if result["importExists"] else "Open/import in Godot after upload.",
    }
    return result


def check_edges(image, asset):
    if not asset.get("transparent"):
        return {"status": "pass", "message": "Not required for opaque/full-background asset."}
    if image.mode not in ("RGBA", "LA"):
        return {"status": "fail", "message": "No alpha channel for edge check."}
    rgba = image.convert("RGBA")
    width, height = rgba.size
    if width <= 0 or height <= 0:
        return {"status": "fail", "message": "Invalid dimensions."}
    edge_pixels = []
    edge_pixels.extend(rgba.getpixel((x, 0))[3] for x in range(width))
    edge_pixels.extend(rgba.getpixel((x, height - 1))[3] for x in range(width))
    edge_pixels.extend(rgba.getpixel((0, y))[3] for y in range(height))
    edge_pixels.extend(rgba.getpixel((width - 1, y))[3] for y in range(height))
    opaque_edges = sum(1 for alpha in edge_pixels if alpha > 8)
    if opaque_edges == 0:
        return {"status": "pass", "message": "Outer edge is transparent."}
    ratio = opaque_edges / max(1, len(edge_pixels))
    if ratio < 0.04:
        return {"status": "warn", "message": f"{opaque_edges} edge pixels have visible alpha; inspect crop."}
    return {"status": "fail", "message": f"{opaque_edges} edge pixels have visible alpha; likely dirty edge/crop."}


def state_with_validations():
    state = read_state()
    validations = {}
    for asset in read_manifest()["assets"]:
        validations[asset["id"]] = analyze_png(asset)
    state["validations"] = validations
    return state


def make_thumbnail(asset):
    target = safe_asset_path(asset)
    source = target if target.exists() else asset_sample_path(asset)
    with Image.open(source) as image:
        image.thumbnail((192, 192))
        output = BytesIO()
        image.save(output, format="PNG")
        return output.getvalue(), source.stat().st_mtime


class DashboardHandler(SimpleHTTPRequestHandler):
    def log_message(self, format, *args):
        print(f"[{datetime.now().strftime('%H:%M:%S')}] {format % args}")

    def do_GET(self):
        path = urlparse(self.path).path
        if path == "/api/health":
            self.write_json({"ok": True, "projectRoot": str(PROJECT_ROOT), "statePath": str(STATE_PATH)})
            return
        if path == "/api/state":
            self.write_json(state_with_validations())
            return
        if path == "/api/thumbnail":
            self.handle_thumbnail()
            return
        self.serve_static(path)

    def do_POST(self):
        path = urlparse(self.path).path
        try:
            if path == "/api/upload":
                self.handle_upload()
                return
            if path == "/api/check":
                self.handle_check()
                return
            if path == "/api/shutdown":
                self.handle_shutdown()
                return
            self.write_json({"error": "Not found"}, status=404)
        except Exception as error:
            self.write_json({"error": str(error)}, status=500)

    def handle_upload(self):
        body = self.read_json()
        asset = find_asset(body.get("assetId", ""))
        if not asset:
            self.write_json({"error": "Unknown asset id"}, status=404)
            return

        data_url = body.get("dataUrl", "")
        prefix = "data:image/png;base64,"
        if not data_url.startswith(prefix):
            self.write_json({"error": "Only PNG data URLs are accepted"}, status=400)
            return

        raw = base64.b64decode(data_url[len(prefix):])
        target = safe_asset_path(asset)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw)

        state = read_state()
        upload = {
            "sourceFileName": body.get("fileName", ""),
            "fileName": target.name,
            "targetPath": asset["relativePath"],
            "bytes": len(raw),
            "updatedAt": now_text(),
            "productionStatus": "Uploaded final art",
        }
        state["uploads"][asset["id"]] = upload
        save_state(state)
        self.write_json({
            "ok": True,
            "assetId": asset["id"],
            "upload": upload,
            "validation": analyze_png(asset),
            "state": state_with_validations(),
        })

    def handle_check(self):
        body = self.read_json()
        asset = find_asset(body.get("assetId", ""))
        if not asset:
            self.write_json({"error": "Unknown asset id"}, status=404)
            return
        check_id = body.get("checkId", "")
        if not check_id:
            self.write_json({"error": "Missing check id"}, status=400)
            return
        state = read_state()
        state["checks"].setdefault(asset["id"], {})
        state["checks"][asset["id"]][check_id] = bool(body.get("checked", False))
        save_state(state)
        self.write_json({"ok": True, "state": state_with_validations()})

    def handle_thumbnail(self):
        query = parse_qs(urlparse(self.path).query)
        asset = find_asset(query.get("assetId", [""])[0])
        if not asset:
            self.write_json({"error": "Unknown asset id"}, status=404)
            return
        data, mtime = make_thumbnail(asset)
        self.send_response(200)
        self.send_header("Content-Type", "image/png")
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Source-MTime", str(mtime))
        self.end_headers()
        self.wfile.write(data)

    def handle_shutdown(self):
        self.write_json({"ok": True, "message": "DungeonFit Art Dashboard server is stopping."})

        def stop_server():
            time.sleep(0.15)
            self.server.shutdown()

        threading.Thread(target=stop_server, daemon=True).start()

    def serve_static(self, path):
        relative = "index.html" if path == "/" else unquote(path).lstrip("/")
        if ".." in relative:
            self.write_json({"error": "Invalid path"}, status=400)
            return
        target = (DASHBOARD_ROOT / relative).resolve()
        root = DASHBOARD_ROOT.resolve()
        if root != target and root not in target.parents:
            self.write_json({"error": "Invalid path"}, status=400)
            return
        if not target.exists() or not target.is_file():
            self.write_json({"error": "Not found"}, status=404)
            return

        content_type = mimetypes.guess_type(target.name)[0] or "application/octet-stream"
        if target.suffix == ".js":
            content_type = "application/javascript"
        data = target.read_bytes()
        self.send_response(200)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def read_json(self):
        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0:
            return {}
        return json.loads(self.rfile.read(length).decode("utf-8"))

    def write_json(self, value, status=200):
        data = json.dumps(value, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


def main():
    parser = argparse.ArgumentParser(description="DungeonFit Art Dashboard local server")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--no-open", action="store_true")
    args = parser.parse_args()

    server = ThreadingHTTPServer(("127.0.0.1", args.port), DashboardHandler)
    url = f"http://127.0.0.1:{args.port}/"
    print("DungeonFit Art Dashboard server")
    print(f"Project: {PROJECT_ROOT}")
    print(f"URL:     {url}")
    print(f"State:   {STATE_PATH}")
    print(f"PID:     {PID_PATH}")
    print("Press Ctrl+C to stop.")
    write_pid_file(args.port)
    if not args.no_open:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
        remove_pid_file()


if __name__ == "__main__":
    main()
