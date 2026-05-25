# DungeonFit MVP Implementation Plan

## Purpose

This document defines the first engineering slice for DungeonFit. The product MVP remains centered on a playable workout-to-dungeon loop, but the implementation should keep core rules outside UI scenes so later systems can extend the first slice instead of replacing it.

## First Playable Loop

The target player-facing workout loop is:

```text
Town
-> enter dungeon
-> choose today's dungeon plan and workout order
-> run 4 to 6 workout stages
-> complete set waves inside each stage
-> bank one chest or reward packet per completed set
-> show a small set summary and ask whether to continue exploring
-> finish today's planned stages
-> show daily summary
-> open all banked dungeon chests together
-> return to Town with updated player state
```

The current engineering slice is a smaller proof of that loop:

- One fixed task template from the in-memory catalog.
- One room run representing one workout stage.
- Four workout sets inside that stage.
- Sets 1 to 3 map to normal waves.
- Set 4 maps to the Boss.
- Completion awards gold and one test equipment result immediately.
- Town receives a run summary after settlement.

This slice verifies the most important product question first: whether manually reporting a real workout set can feel like meaningful dungeon progress. It is intentionally not the final daily dungeon structure yet.

## Target Daily Dungeon Flow

The full MVP direction should treat `Enter Dungeon` as a planning step, not an immediate jump into a room.

1. Town opens the dungeon selection / planning page.
2. The player builds today's route by adding 4 to 6 target-area dungeon slots, such as chest, shoulders, back, abs, legs, arms, or other categories to be refined later.
3. The same target-area dungeon can appear multiple times in the same route, such as chest -> shoulders -> chest -> arms.
4. Each route slot resolves into one workout room from that target area's content pool.
5. Each stage is one real workout action, such as a `4 x 8-16` movement.
6. Inside a stage, each set is one Wave-driven room segment.
7. Completing a set banks one chest or reward packet, but does not force a full loot-opening animation immediately.
8. After each set, the game shows a small summary and asks whether the player wants to continue exploring, so real body state can override the plan.
9. After the planned 4 to 6 stages end, the game shows a daily summary and opens all banked dungeon chests together.

The concentrated chest opening at the end is intentional: it gives the highest reward feedback after the real workout session instead of scattering the payoff too thinly across every set.

## Route And Quest Rules

`DungeonPlan` is the player's daily workout route, not an NPC quest. The player can always enter the dungeon and create or use a free training route without accepting any quest.

The route is a sequence of target-area dungeon slots rather than a single selected dungeon. Repeated target areas are valid and important for real training plans. For MVP implementation, each slot stores the selected target area, sets, reps, music choice, and rest seconds. It resolves into a generic `TaskTemplate` for the Room Challenge, without forcing a specific real-world exercise such as a dumbbell press.

NPC quests are optional bonus objectives:

- A quest should usually target one training category, such as chest, back, legs, core, or arms.
- A quest may require one or two matching workout stages, but should not force the whole route.
- Accepted quests can add an `Active Quest Bonus` to the route planning page.
- Quest rewards are extra rewards layered on top of the base dungeon rewards.
- The current default route is best understood as a free player-built dungeon route placeholder, not a quest route.

## Implementation Order

Do not start by placing room rules inside button handlers or scene scripts. The implementation order is:

1. Build the minimal C# core models and room rules.
2. Build one room-run gameplay service around those rules.
3. Connect the thinnest Town, Room Challenge, and Room Result UI scenes.
4. Return rewards into player state.
5. Add a thin dungeon planning and daily run layer after the single-room slice is stable.
6. Add persistence, content loading, and broader systems after the loop is playable.

## Layer Boundaries

### Core

Core owns state shapes and rules that should not depend on Godot UI scenes.

Initial models:

- `PlayerState`
- `TaskTemplate`
- `RoomRun`
- `RoomProgress`
- `CompletionResult`
- `RewardBundle`
- `EquipmentItem`

### Gameplay

Gameplay owns use-case flow over the core models.

Initial services:

- Start a room run from a task template.
- Complete a set.
- Record a partial set.
- Skip a room.
- Resolve room rewards.

The first service may be small, but UI code should call it rather than directly mutating gold, equipment, Boss state, or reward results.

### UI

UI scenes own presentation and player input.

Initial scenes:

- `Town`
- `RoomChallenge`
- `RoomResult`

UI may format state for display, but reward generation, wave progression, and player-state mutation should remain outside the view scripts.

### Room Challenge UI Direction

The first Room Challenge layout should already read as a dungeon battle room rather than a workout form.

Its first visual skeleton is:

1. Header with room name, challenge name, pause control, and wave progress markers.
2. Battle stage with the player on one side and the current wave enemy or Boss on the other.
3. Beat Flow panel that visualizes workout rhythm and music rhythm.
4. Workout status strip for action name, reps or duration, set progress, rest state, and current music.
5. Manual completion actions surfaced at the point the room flow needs player reporting.

For the first playable slice, use placeholder battle figures, a placeholder rhythm waveform, and Godot UI containers. Do not block the room loop on final pixel art, Spine animation, or real audio waveform analysis.

The first manual report entry point is a contextual panel. Keep completion actions hidden during an active Wave, then surface them when the Wave flow reaches Break / Rest and needs the real set result. Do not add a player-facing "Set Finished" button as the trigger for this transition.

### Wave Indicator Rules

Wave is the active-set rhythm indicator. It is not a touch interaction surface.

- One rep maps to one target wave cycle.
- One set contains the target rep count in wave cycles.
- A `4 x 16` task runs 16 wave cycles, enters Break / Rest, accepts the set report, and repeats that structure across four sets.
- The Wave shape indicates Push and Release timing for the real movement.
- The MVP does not ask the player to hold, release, or track the Wave on screen.
- Wave speed is tied to music BPM plus a beats-per-rep parameter.

The first prototype uses parameterized timing instead of a real track:

- `120 BPM`
- `4 beats per rep`
- `16 reps per set`

Action-specific wave shapes, audio sync drift handling, and music metadata loading remain later work.

## Expansion Checks

The first slice must leave clear extension points for:

| Later system | First-slice boundary to keep |
|---|---|
| Multiple NPC tasks | Task data lives in `TaskTemplate`, not a UI scene |
| Multi-room dungeons | `RoomRun` represents one room outcome cleanly |
| Dungeon and shop chests | Reward resolution has an explicit reward source |
| Equipment affixes and rerolls | Equipment results are data, not hardcoded label text |
| Idle gold and shop spending | Gold belongs to `PlayerState` |
| Fatigue and recovery | Completion results can feed later workout-state rules |
| Save/load | Player-facing state is collected under `PlayerState` |
| Analytics | Gameplay actions have clear use-case boundaries to instrument |

Completion states should be modeled as data rather than button names. The first slice can start with:

- Completed.
- Partial.
- Skipped.

Later completion adapters can add reduced-load completion, phone sensors, wearables, or camera pose input without replacing the room-run flow.

## Deliberately Deferred

Do not build these before the first room loop is playable:

- Offline idle gold calculation.
- Shop UI.
- Shop chest tables.
- Affix reroll UI.
- Music unlock shop.
- Fatigue balancing.
- Full NPC task generation.
- Full JSON content pipeline.
- Full save-file pipeline.
- Complex event bus or dependency injection framework.

The first implementation should be extensible but not framework-heavy.

## Suggested Code Shape

Start with a small C# layout:

```text
Assets/Scripts/
  Core/
    Models/
    Rules/
  Gameplay/
  UI/
```

The project may add `Data`, `Save`, and `Services` folders when those systems have real code to own.

## First Acceptance Criteria

### Rules

- A four-set task creates a room run.
- The first three completed sets advance normal waves.
- The fourth completed set clears the Boss wave.
- A completed room returns a temporary reward bundle with gold and one test equipment result for the current single-room slice.
- The target daily dungeon should bank one chest or reward packet per completed set and open all banked dungeon chests on the daily summary page.
- Partial and skipped outcomes produce distinct room results.

### Playable Slice

- The player can start the fixed task from Town.
- The Wave flow reaches Break / Rest before manual completion actions appear.
- The player can complete a room through manual set reports during Break / Rest.
- The room result shows the Boss clear and rewards.
- Returning to Town shows the updated gold total.
- The next slice should insert a dungeon planning page between Town and Room Challenge.

## Town Direction

The Town should read as a home base scene first and a menu second. Use the moonlit pixel-art render in `Assets/doc/references/ui/town_render.png` as mood reference, but avoid persistent building-description panels on the main screen.

MVP Town layout:

- Top status bar: player portrait/name/level, gold, and settings.
- Main town scene: tavern, herb shop, general store, notice board, church, and a central fountain or shrine as visual anchors.
- Lower idle exploration panel: character idle/walking animation, small coin or exploration feedback, and a simple "exploring" state.
- Primary bottom action: enter dungeon.

Town interaction rules:

- Default state should be clean; buildings do not show permanent labels or multi-word descriptions.
- Tapping a building may show one small selected-state label or bottom action drawer.
- MVP can keep most buildings decorative while only `Enter Dungeon` and, later, `Claim Idle Reward` are functional.
- Town must not compete with the Room Challenge screen for gameplay feedback; it is the calm return state between dungeon runs.

## Current Progress

Recorded on May 23, 2026. Updated on May 25, 2026:

- The project baseline is Godot 4.6.2 .NET with Android-first portrait settings.
- C# core models exist for player state, task templates, room runs, room progress, rewards, reward source, completion results, and equipment items.
- A small room-run service exists, and room rewards now flow through `LootRoller` / `LootTable`.
- The Room Challenge scene is split between view binding, `RoomPhaseController`, `RoomResultPresenter`, and `RoomAudioBridge`.
- `GameSession` now owns the shared `PlayerState`, selected plan, active run, and latest summaries; the main scene focuses on scene switching and event wiring.
- A first Town scene exists with top player/gold status, clean building placeholders, an idle exploration panel, and an `Enter Dungeon` primary action.
- A first Dungeon Plan scene exists between Town and Room Challenge. It now behaves as a dungeon route builder first: the player taps Chest, Legs, Back, Shoulders, Core, or Arms to append target-area slots to today's route, and repeated areas are allowed.
- The Room Challenge scene now receives the shared player state from Main and returns to Town from the result panel.
- Room results now produce a small run summary at room settlement time so Town can show the latest banked reward after returning; the return button only handles navigation.
- A thin `DungeonRun` / `DungeonRunService` now tracks the active plan, completed stage summaries, and current stage index.
- A first Set Summary scene exists after each completed stage, showing stage result, completed sets, stage gold, banked chest count, and continue/return choices.
- A first Daily Summary scene exists after a completed route, aggregating completed stages, sets, gold, and equipment rewards. It requires `Open All` before returning to Town.
- `BankedReward` records one dungeon chest packet per completed set when a stage result is recorded, including a `RewardBundle` payload. `PlayerState` is applied only when the player uses `Open All` on the Daily Summary page, not when a room ends or when returning to Town.
- A `--flow-smoke-test` diagnostics path verifies default plan stage progression, banked reward accumulation, and reward application timing.
- The test room content now comes from an in-memory `TaskCatalog`; Room Challenge receives a `TaskTemplate` from Main instead of hardcoding its own task data.
- Town shows the current `Today Route` summary, while Dungeon Plan shows the selected target-area route and resolved room details.
- Dungeon Plan now supports a minimal target-area route selector before a run starts: the player builds a 4 to 6 slot route, can repeat the same target area, and the selected route is locked once the run begins.
- The Dungeon Plan engineering UI now follows the dungeon entrance layout: six target-area dungeon buttons append slots to the route, the route list shows selected and empty slots, and the adventure button stays disabled until at least four slots are selected.
- A fresh session now starts with an empty route so the lower route list shows empty slots first; test/default routes are created explicitly only for diagnostics or once the player selects target areas.
- The route list now previews each selected slot's player-chosen target area, sets, reps, music, and rest seconds. Specific exercise names are intentionally not assigned at the dungeon entrance layer.
- Tapping a dungeon button opens a small settings dialog before adding the route slot. The dialog currently supports sets, reps, music choice, and rest duration presets of 60, 90, 120, and 300 seconds.
- Room Challenge now receives route position context and displays the current room number out of the total route. Set Summary now shows the completed room number and previews the next planned room when one remains.
- Route data preparation has been split into focused catalogs and rules: `DungeonCategoryCatalog` owns target-area dungeon metadata, `MusicCatalog` owns available tracks and BPM, `DungeonRouteRules` owns route validation, set/rep limits, rest presets, and timing profile creation, and `TaskCatalog` now only converts route slots into generic room challenge templates.
- Wave timing now uses `WorkoutTimingProfile`, and Room Challenge has a first functional Break / Rest countdown using the selected rest seconds. The report panel appears after the rest countdown completes.
- Room Challenge now has an engineering Rest control panel shown during Break / Rest, with `Ready Now` to end rest early and `+30s` to extend the countdown based on real body state.
- Route slot setup UI has been split into `RouteSlotDialogView`, implemented as a mobile-style in-scene overlay / bottom sheet rather than a desktop confirmation dialog. `DungeonPlanView` now only opens the dialog and receives confirmed route slots.
- Dungeon Plan rendering is split into `DungeonTypeGridView`, `DungeonRouteListView`, and `DungeonPlanSummaryPresenter`.
- Room Challenge battle display now has a first actor layer: `BattleActorView`, `BattleEncounterView`, and `EnemyCatalog`. It still uses engineering token panels, but player/enemy/Boss visuals can now be replaced without rewriting room phase logic.
- Knight and Skeleton spritesheets are now organized under `Assets/Art/Actors/` and loaded at runtime into `AnimatedSprite2D` / `SpriteFrames`. Idle, Attack01, Hurt, and Death are connected to battle actor states.
- MVP save/load now persists player gold, inventory, selected route, active run stage results, banked rewards, daily claim state, and the last run summary to `user://save.json`.
- Town settings now includes player-facing save management: manual save, delete current save, and a current save status summary. Deleting the save resets the active session.
- Route music now uses stable `MusicId` values instead of display names, while UI text is resolved from `MusicCatalog`.
- Full route reordering, custom workout creation, category filters, quest bonus selection, and JSON content loading remain deferred.
- The product target is now documented as Town -> dungeon planning -> 4 to 6 ordered workout stages -> per-set chest banking -> daily summary -> concentrated chest opening -> Town.
- The Beat Flow panel now has a first Wave indicator prototype: a BPM-driven target wave, a current rhythm marker, and automatic Break after the rep-count wave cycles finish.
- The current UI uses placeholder fighters. Final art, Spine animation, action-specific Wave shapes, and exact audio sync are not integrated.
- The current playable loop is Town -> Dungeon Plan -> current stage Room Challenge -> result -> Set Summary -> Dungeon Plan with updated route progress -> Daily Summary -> Open All -> Town. MVP per-set chest banking exists; full chest-opening animation is not implemented yet.
- Ending training from Set Summary now routes into Daily Summary first. Town gold only changes after `Open All`, so players cannot accidentally bypass reward claim by returning to Town early.
- JSON content loading, analytics, idle gold, shop, full chest tables, affix rerolls, music unlocks, and fatigue rules remain deferred.
