# DungeonFit Android C# Setup

## Baseline

DungeonFit starts Android-first with:

- Godot 4.6 project settings.
- C# as the primary scripting language.
- Portrait viewport baseline at 1080 x 1920.
- `canvas_items` stretch mode with `expand` aspect handling for mobile UI work.
- Mobile renderer enabled in `project.godot`.

The bootstrap scene is `res://Assets/Scenes/Main.tscn`. It exists to verify that the project opens, C# compiles, and Android deployment has a known entry point before gameplay scenes are added.

## Local Tooling

Install and configure these on each development machine:

1. Godot 4.6 .NET editor.
2. .NET SDK 8.
3. OpenJDK 17.
4. Android Studio and Android SDK packages required by Godot:
   - Android SDK Platform-Tools 35.0.0 or later.
   - Android SDK Build-Tools 35.0.1.
   - Android SDK Platform 35.
   - Android SDK Command-line Tools latest.
   - CMake 3.10.2.4988404.
   - Android NDK r28b.

In Godot Editor Settings, configure:

- `Java SDK Path` to the OpenJDK 17 install.
- `Android SDK Path` to the SDK directory that contains `platform-tools/adb`.

Keep these paths as local editor settings. Do not commit machine-specific SDK paths into the project.

## Android Workflow

1. Open the project with the Godot 4.6 .NET editor.
2. Let Godot import resources and build the C# project.
3. Add an Android export preset in the editor.
4. Use APK exports for device iteration.
5. Use AAB exports and a release keystore for Google Play delivery.

Godot currently documents C# Android export as supported but experimental. Keep early Android verification frequent while core systems are still small.
