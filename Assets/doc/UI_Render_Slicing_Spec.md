# UI Render Slicing Spec

Source folder: `C:\Users\JSrad\Desktop\健身地城\草圖`

The AI render set is the visual target for the Godot UI rebuild. Use these renders as production art sources, not only loose references. Each page should keep dynamic text and state in Godot while using sliced PNG assets for backgrounds, frames, buttons, and decorative motifs.

## Global Rules

- Keep source renders outside the repo; commit only optimized/cropped slices under `Assets/Art/UI/<PageName>/`.
- Use transparent PNG slices for cards, icons, frames, button bodies, corner ornaments, and selection marks.
- Use full opaque page crops only as temporary reference assets or large backgrounds with no baked dynamic state.
- Any asset used as a resizable panel/button should be imported for nine-slice usage and wrapped by existing `DungeonFitUi` helpers where possible.
- Do not bake player state, counts, route names, or button text into final UI assets. Those must remain Godot labels.

## First Standard Page: DungeonPlan

Source render: `選擇地城渲染圖.png` (`941x1672`)

Current first-pass slices:

| Asset | Path | Use | Notes |
|---|---|---|---|
| Full reference | `Assets/Art/UI/DungeonPlan/RenderSlices/dungeon_plan_reference_full.png` | Visual QA only | Do not use directly as live UI background. Contains baked text/state. |
| Portal stage | `Assets/Art/UI/DungeonPlan/RenderSlices/bg_portal_stage.png` | Reference/background candidate | Contains baked cards; needs cleanup before final background use. |
| Route panel reference | `Assets/Art/UI/DungeonPlan/RenderSlices/panel_route_reference.png` | Panel slicing reference | Needs clean panel frame slice without baked route rows. |
| Attack button reference | `Assets/Art/UI/DungeonPlan/RenderSlices/button_attack_reference.png` | Button slicing reference | Needs text-free button body for final use. |
| Dungeon card references | `card_*_reference.png` | Icon/card art reference | First pass only; final card should use icon art without baked label/check. |

## Required Final Slices

For `DungeonPlan`, produce these final shipping assets:

- `bg_dungeon_portal_clean.png`: background without baked selectable cards or route rows.
- `panel_route_9slice.png`: text-free bottom route panel.
- `button_attack_9slice.png`: text-free primary button body.
- `button_back_9slice.png`: text-free back button body or reuse common secondary button.
- `card_dungeon_9slice.png`: unselected dungeon card frame.
- `card_dungeon_selected_9slice.png`: selected dungeon card frame.
- `mark_selected.png`: check badge.
- `icon_chest.png`, `icon_shoulders.png`, `icon_back.png`, `icon_arms.png`, `icon_core.png`, `icon_legs.png`: transparent icons.

## TSCN Pattern

- Main page scene owns static layout and named containers.
- Dynamic repeated UI uses child scenes:
  - `DungeonTypeCard.tscn`
  - `DungeonRouteSlotRow.tscn`
  - `ExerciseChoiceCard.tscn`
- C# view classes instantiate child scenes, call `Initialize(...)`, and subscribe to events.
- C# must not build visual layout trees for stable UI surfaces.
