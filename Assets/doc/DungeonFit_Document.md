# DungeonFit Design Document

> Last updated: 2026-05-26
> Encoding: UTF-8

## Project Direction

DungeonFit is an Android-first Godot 4 .NET / C# workout RPG MVP.

The current product loop is:

```text
Town
-> Dungeon Plan
-> select 4 to 6 target-area dungeons
-> enter Room Challenge
-> complete set waves and rest phases
-> bank stage rewards
-> Set Summary
-> continue the selected route
-> Daily Summary
-> Open All
-> Town
```

The core design principle is that real workout flow comes first. The game should guide rhythm, rest, reward, and progression without forcing the player into awkward input timing.

## Dungeon Route

The Dungeon Plan page is the in-game dungeon selection page. The player can freely build today's route by selecting 4 to 6 target-area dungeons.

Allowed examples:

- Chest -> Shoulder -> Chest -> Arm
- Core -> Core -> Leg -> Leg -> Leg
- Chest -> Shoulder -> Back -> Chest -> Chest -> Shoulder

Repeated dungeon types are valid and intentional. A route slot represents a target-area dungeon, not a fixed exercise.

Each `DungeonRouteSlot` stores:

- `DungeonTypeId`: chest, shoulders, back, legs, core, arms.
- `TargetSets`: selected set count.
- `TargetReps`: selected reps per set.
- `MusicId`: stable music identifier.
- `RestSeconds`: 60, 90, 120, or 300 seconds.

UI display names must be resolved from content catalogs. Save data should not depend on display text.

## Wave Logic

Wave is a visual tempo guide, not an input game.

The player does not press, drag, or trace the Wave panel. The Wave only indicates training rhythm:

- One rep equals one complete wave cycle.
- One set contains `TargetReps` wave cycles.
- A `4 x 16` route slot means 4 sets, each with 16 wave cycles.
- After a set ends, the room enters Break / Rest.

Wave speed is derived from music BPM and a training timing profile. For strength training, the default should avoid overly fast aerobic pacing. The current direction is metadata-driven timing, not rhythm-game precision.

Wave peak also drives battle presentation. Each rep emits an attack anticipation event shortly before the guide reaches the top of the wave, so the player actor can play the attack wind-up early. The peak event itself is treated as the hit timing, and enemy Hurt is triggered there. This remains non-interactive; the Wave is still a timing guide rather than an input target.

## Room Challenge

Room Challenge is controlled by an explicit phase controller:

- `ActiveWave`
- `RestCounting`
- `AwaitingReport`
- `Result`

`RoomChallengeView` should stay focused on Godot node binding and visual refresh. It delegates:

- Phase transitions to `RoomPhaseController`.
- Result panel behavior to `RoomResultPresenter`.
- Music volume and phase reactions to `RoomAudioBridge`.
- Battle actor display to `BattleEncounterView`.

The intended flow is:

```text
Wave completed
-> Rest countdown
-> Ready now
-> player reports completed or withdraw
-> next wave active
-> 4/4 completed
-> Boss Cleared
-> Continue
-> Set Summary
```

If the player chooses to end training from Set Summary, the flow must go to Daily Summary first, not directly back to Town. Gold is applied only after Daily Summary `Open All`.

## Battle Actor Display

Room Challenge now has a replaceable battle actor layer:

- `BattleActorView`: owns one actor token and its display state.
- `BattleEncounterView`: owns player/enemy presentation, boss label, and boss HP bar.
- `EnemyCatalog`: maps dungeon type to normal enemy and boss names.

Current actor states:

- Idle
- Active
- Rest
- Evading
- Moving
- Hit
- Defeated
- Victory

This is still engineering UI. The player and enemy are shown with panel tokens, but the structure is ready for static images, sprite sheets, Spine, or future Godot animation nodes without rewriting the Room Challenge phase logic.

Current imported actor assets:

- Player uses Knight spritesheets under `Assets/Art/Actors/Player/Knight/`.
- Enemy and Boss currently share Skeleton spritesheets under `Assets/Art/Actors/Enemies/Skeleton/`.
- Runtime animation frames are built from horizontal `100 x 100` PNG strips.
- Connected animations: Idle, Attack01, Hurt, Death.
- Preserved but unused this round: Walk, Block, Attack02, Attack03, overview sheets.
- Battle actors now use absolute positions inside the battle stage instead of an HBox layout. Player is anchored toward the lower-left combat position, while enemy/Boss is anchored toward the right-side combat position. This prepares the next pass for beat-synced attack movement.
- Beat-synced attack display is split into wind-up and hit timing: Attack01 starts before the Wave peak, enemy Hurt happens at the Wave peak, and attack animation does not loop during a single rep.
- Rep-time combat feedback switches actor states from combat results: player attack, player hurt, evasion, enemy attack, enemy hurt, defeated, and moving after a defeated enemy.

The result Continue flow is:

```text
RoomResultPresenter
-> RoomChallengeView.RoomContinueRequested
-> Main.RecordStageResult
-> SetSummaryView
```

The result panel still keeps a fallback click / keyboard input path so a button focus issue does not trap the player.

## Non-Failure Combat Economy

DungeonFit combat is a reward-efficiency system, not a fail state. The player's real training flow must continue even when the character cannot defeat an enemy.

- Each rep hit timing resolves one combat hit.
- Each completed real-world set seals the result of that combat set.
- If player HP is above 0, the character attacks on rep hit.
- If player HP is 0 or negative, the character switches to evasion and stops attacking.
- Enemies attack during the set on deterministic pseudo-random rep timings.
- Normal enemies attack less often; Boss enemies attack more often.
- HP can go negative, but the run clamps it to `-MaxHP`.
- The dungeon route does not end when HP reaches 0 or a negative value.
- Every resolved set gives positive gold.
- Only defeated enemies create a chest.
- Enemies that survive the set give gold-only rewards and no chest.
- Chest contents remain sealed until Daily Summary chest opening.

MVP combat stats are intentionally small:

- Player base HP starts at 24.
- Player base Attack starts at 3.
- Equipment Attack and HP add onto those base values.
- Dungeon enemies use fixed normal and boss HP/Attack values per dungeon type.
- Player level adds slow base growth: MaxHP +2 per level, Attack +1 every two levels.
- Dungeon level exists on room templates and is fixed to Lv.1 for the first balance pass.
- Enemy level follows dungeon level. Future dungeon level scaling can increase HP by roughly 8% per level and Attack by +1 every three levels.

The first combat formula is intentionally narrow:

```text
Rep hit damage = max(1, Attack + EquipmentScore / 6)
Evading damage = 0
Evading damage taken = 1
```

If the enemy is defeated before the set ends, the enemy remains at 0 HP and no longer attacks. Remaining reps continue as training and are presented as moving toward the next enemy.

Healing, lifesteal, buffs, defense, hit rate, critical hits, elements, enemy skills, and equipment enhancement are reserved for later passes. Healing hooks may exist in code, but v1 does not require healing equipment to be available.

EXP is awarded at room completion, not daily summary:

- Each completed set grants 8 EXP.
- Each chest set grants +4 EXP.
- A Boss chest grants an additional +12 EXP.
- Gold-only sets still grant the base set EXP.
- Level-up does not refill active run HP.

## Reward And Loot

Reward flow is now separated into preview, banked rewards, and claimed rewards.

- `StagePreviewReward`: result-page preview concept.
- `BankedReward`: stored reward packet before daily claim.
- `ClaimedReward`: reward applied to `PlayerState`.
- `DungeonChest`: dungeon-run chest source.
- `ShopChest`: shop chest source, kept separate from dungeon chest logic.

MVP reward rules:

- Each completed set creates a positive reward packet.
- A chest is created only when the enemy is defeated.
- If the enemy survives, the set stores a gold-only reward.
- Chest eligibility and reward packet existence must remain separate concepts.
- Damage and HP update during reps; gold and chest eligibility are still sealed at set end.
- Banked rewards are not applied immediately.
- Daily Summary `Open All` applies banked rewards to `PlayerState`.
- Stage result gold and sealed chest eligibility are generated through combat resolution and `LootRoller` / `LootTable`, not by hard-coded view logic.
- Daily Summary counts only real chest rewards as pending chests; gold-only rewards still contribute to total gold.

The current loot table is still intentionally simple. Full equipment rarity, affixes, rerolling, and shop chest balancing are future work.

## Save And Load

Save/load now uses `SaveService` and `SaveGameState`.

Save path:

```text
user://save.json
```

Current saved data includes:

- Player gold.
- Player level, EXP, and next-level threshold.
- Player inventory and equipment loadout.
- Selected dungeon route, only when it belongs to an active run.
- Active run existence, including the edge case where the player started a route but has not cleared the first room yet.
- Active run current HP.
- Active run stage results.
- Banked rewards.
- Daily reward claimed state.
- Last run summary.
- Notice Board refresh key for the current 24-hour short-term quest board.
- Active short-term quest state.

Save is triggered when:

- The route is updated.
- A run starts.
- A stage result is banked.
- Daily rewards are claimed.
- The daily run is completed and the player returns to Town. At this point the selected route is cleared so the next dungeon entry starts from a blank plan.

The Town settings page also exposes player-facing save management:

- Manual save.
- Delete current save.
- Current save status summary.

Manual save uses the same save payload as auto-save. Delete save removes `user://save.json` and resets the current session to a clean new-player state.

Flow smoke tests disable persistence so diagnostics do not pollute the user's save file.

## Music And BPM

Route slots use `MusicId`, not display name.

`MusicCatalog` is responsible for resolving metadata:

- Stable id.
- Display name.
- BPM.
- Godot resource path.
- Beat offset.
- Usable start / end.
- Loop start / end.
- Volume offset.

`WorkoutMusicPlayer` currently supports:

- Active phase playback.
- Random usable segment start.
- Loop region support.
- Active volume.
- Rest ducking without stopping music.
- Result / exit fade out.

Known future verification:

- Manual loop-point listening.
- Android device playback behavior.
- Bluetooth latency.
- Per-track loudness balancing.
- Beat offset tuning.

## Town

Town is the main hub. MVP functional focus:

- Player status and gold.
- Enter Dungeon.
- Idle exploration panel.
- Last banked reward summary.

Most buildings can remain decorative until their systems are implemented.

Planned buildings:

- Tavern: equipment review, loadout slots, warehouse inventory, selling, locking.
- Herb shop: paid healing, potion purchases, and room-challenge supply items.
- Blacksmith: equipment purchases, enhancement, and extending equipment usable level range.
- Notice board: optional NPC quests.
- Church: long-term oath quests, basic NPC dialogue, story, prayer.
- Moonlight Fountain: daily free recovery and today's blessing buff.

## Remaining Core MVP Systems

These systems are still required for the basic MVP because they support HP recovery, gold sinks, long-term goals, and Town return-state rewards.

### Moonlight Fountain MVP

- Show current player HP.
- Allow one free recovery per day.
- Restore a fixed percentage of HP.
- Let the player choose one blessing buff for today.
- Prevent repeat use after today's free recovery / blessing is consumed.
- Save the daily used state and selected blessing.

### Herb Shop MVP

- Show current player HP.
- Let the player spend gold to recover HP.
- Sell small potions.
- Allow purchased potions to be used during Room Challenge.
- Enforce a daily purchase limit.
- Save shop purchase counts and owned supplies.

### Idle Reward MVP

- Show the character exploring outdoors in the lower Town panel.
- Accumulate small gold rewards at a fixed interval.
- Let the player claim accumulated idle gold.
- Apply a maximum accumulation cap.
- On reload, grant partial offline accumulation based on elapsed time.
- Save idle reward timestamps and unclaimed amount.

### Blacksmith MVP

- Show current equipment and inventory.
- Let the player select one equipment item.
- Spend gold to enhance the selected item.
- Enhancement grants Power +1 or increases the main stat by +1.
- Equipment has an enhancement cap.
- Spend gold to extend the equipment usable level range.

### Equipment Level Rules

- Equipment has a recommended level range.
- When the player's level exceeds the range, equipment effects decay.
- The Blacksmith can extend the usable level range with gold.

### Church MVP

- Show long-term oath quests.
- Objectives should accumulate over multiple days or many dungeon runs.
- Completion rewards can include titles, large gold payouts, or rare equipment.
- Only one long-term oath quest can be active at a time.
- Abandoning an oath resets its progress.
- Church quests should include basic NPC dialogue and story framing.

## NPC Quests And Route

Free training and NPC quests are separate.

The player can enter dungeons without accepting quests. If the player accepts a notice-board quest, it adds optional bonus conditions and rewards, but it should not force the entire route.

Quest direction:

- An NPC usually focuses on one target area.
- A quest may ask for one or two matching slots.
- Completing the condition grants extra rewards.
- The base dungeon route remains player-built.

## Notice Board Screen

The Notice Board is the short-term quest screen. It should feel like a large wooden board overlaying the Town view: the board is the focus, but parts of the Town background can remain visible around it.

Screen structure:

- Top area: player status remains visible in the Town header.
- Main board area: six short-term quest cards arranged in a 2 x 3 grid.
- Detail area: selected quest information appears below the quest cards.
- Bottom action: a large Enter Dungeon button remains available, so the player can proceed without accepting a quest.

Quest card content:

- Short quest title.
- NPC portrait or token.
- Small quest-type icon, such as sword, herb, chest, or healing cross.
- Accepted state marker, such as a small sword icon.
- Completed or claimable state marker, such as a check mark.

Quest detail content:

- Quest title.
- World-flavored short story text.
- Quest requirement.
- Current progress, such as `0/1`.
- Larger NPC portrait.
- NPC name.
- Primary button:
  - `Accept Quest` when available.
  - `Accepted` when already active.
  - `Claim Reward` when completed and unclaimed.

MVP quest behavior:

- The board can show six predefined short-term quests.
- Short-term quests are fixed for 24 hours, then refresh as a set.
- MVP refresh uses the local calendar date as the refresh key.
- The player can select a quest card to inspect details.
- The player can accept every short-term quest on the board, but accepting is optional.
- Accepted quests should later appear as active quest bonuses on Dungeon Plan.
- When a completed room matches an accepted short-term quest's target dungeon type, the quest progress increases by 1 up to the required amount.
- Completed short-term quests can be reported on the Notice Board.
- MVP short-term quest rewards pay gold directly into `PlayerState`.
- Claimed quests are marked as completed and remain visible until the future 24-hour board refresh clears or replaces the board.
- When the local refresh date changes, accepted, completed, and claimed short-term quest states are cleared. Player gold already earned from claimed quests is not reverted.
- The player may still enter the dungeon without accepting any quest.
- Quest rewards should stay simpler than Church oath rewards: gold, small chests, daily resources, or modest bonus items.

Implementation direction:

- Add a `NoticeBoardView` scene or view class.
- Add short-term quest definitions through a catalog, separate from Church oath quest definitions.
- Save active short-term quest states separately from long-term oath quest state.
- Keep Notice Board routing simple: Town -> Notice Board -> Dungeon Plan or Town.

## Church Long-Term Oath Quests

Church quests are long-term oath quests, separate from notice-board quests.

The notice board should stay focused on short-term optional NPC commissions: today's route guidance, small bonus rewards, and quick completion. The Church owns longer goals that can take multiple days or many dungeon runs to finish.

Core rules:

- The player can have only one active long-term oath quest at a time.
- When no oath quest is active, the Church offers three random candidates from predefined quest entries.
- The player chooses one of the three candidates.
- Progress is saved across days and sessions.
- The quest does not force today's dungeon route, but it can encourage a training direction.
- After completion, the player claims a high-value reward and the Church can generate a new three-choice candidate set.

Example oath quest types:

- Train on consecutive days for 3 days.
- Complete 100 total workout sets.
- Complete 10 chest dungeon rooms.
- Focus sweep chest dungeon: defeat 10 chest dungeon bosses.
- Open 20 dungeon chests.
- Complete 5 daily routes that include a leg dungeon.
- Complete 3 full 6-room daily routes.

Reward direction:

- Church oath quest rewards should be stronger than notice-board quest rewards.
- Rewards can include higher-tier dungeon chests, rare equipment, high-affix equipment, special music unlocks, appearance rewards, or permanent progression materials.
- Notice-board rewards can remain simpler: gold, small chests, and daily resources.

Tier direction:

- Tier 1: short long-term goals, roughly 2 to 3 training days. Rewards can include better dungeon chests, more gold, and white or blue equipment.
- Tier 2: medium goals, roughly around a week. Rewards can include blue or purple equipment, rare chests, or high-affix equipment.
- Tier 3: longer challenges. Rewards can include purple equipment, special music, appearance rewards, or permanent growth materials.

Future data shape:

- `LongTermQuestDefinition`: fixed quest entry data such as id, title, description, tier, objective type, target dungeon type, required amount, and reward.
- `ActiveLongTermQuest`: selected quest id, current progress, start date, completion state, and claim state.
- `LongTermQuestCatalog`: predefined quest pool and candidate generation.

## UI Event Rule

Main views should use C# events:

- `TownView.EnterDungeonRequested`
- `DungeonPlanView.StartAdventureRequested`
- `DungeonPlanView.BackToTownRequested`
- `DungeonPlanView.DailySummaryRequested`
- `SetSummaryView.ContinueRequested`
- `SetSummaryView.ReturnToTownRequested`
- `DailySummaryView.OpenAllRequested`
- `DailySummaryView.ReturnToTownRequested`

`WaveIndicatorView.SetWaveCompleted` may remain a Godot signal for now because it is an internal Control component signal.

## Current Implementation Status

Implemented:

- Town -> Dungeon Plan -> Room Challenge -> Set Summary -> Daily Summary -> Town loop.
- Dungeon route selection with repeated target-area dungeons.
- Per-slot set count, reps, music, and rest seconds.
- Rep-time combat with player/enemy HP, evasion, deterministic enemy attacks, kill-gated chest eligibility, and gold-only rewards.
- Player Level / EXP / Attack / MaxHP scaling.
- Dungeon loot profiles, rarity tables, unique equipment instance ids, and sealed Daily Summary chest reveal.
- Room Challenge phase controller split.
- Result panel presenter split.
- Room audio bridge split.
- Battle actor / encounter display split.
- Tavern equipment MVP with character summary, loadout slots, inventory filters/sort, equip, unequip, sell, lock, and save-management panel.
- Notice Board MVP skeleton with six short-term quest cards, detail panel, optional multi-quest acceptance, room-completion progress updates, ready-to-report state, direct gold reward claiming, claimed state persistence, local-date 24-hour refresh, and Dungeon Plan active quest bonus display.
- Dungeon Plan helper presenters for grid, route list, and summary state.
- Banked rewards with Open All claim timing.
- Save/load service for MVP progression.
- Music metadata through `MusicId`.
- Flow smoke test for route progression and reward claim timing.

Still MVP / placeholder:

- Visual art and final mobile polish.
- Full loot table balancing.
- Equipment affixes and rerolling.
- Chest-opening animation.
- Moonlight Fountain daily recovery and blessing.
- Herb Shop paid healing and room potion supply.
- Blacksmith equipment enhancement and usable-level extension.
- Idle reward accumulation and offline catch-up.
- Church long-term oath quest implementation.
- Notice Board final visual polish, visible refresh countdown, and broader reward variety.
- Android export/device verification.
- Precise music loop points and latency calibration.

## Current Risks

High:

1. Save/load needs real editor/device verification after longer play sessions.
2. Loot is structured but still simple; balancing and chest presentation are incomplete.
3. Music metadata exists, but loop points and beat offsets are not fully curated.
4. Android export and device playback have not been validated in this pass.
5. Notice Board refresh currently uses local device date. Future online versions may need server time or anti-clock-tampering handling.

Medium:

1. Some scene default text is still engineering UI and English.
2. Dungeon Plan and Route Slot dialog are split enough for MVP, but final mobile visuals still need art-driven layout.
3. Reward grouping in save data assumes one reward packet per completed set; keep chest eligibility separate from gold-only rewards as future rewards become more variable.
4. `WaveIndicatorView` still uses a Godot signal by design; revisit only if event debugging becomes painful.
5. Notice Board progress currently advances when a matched room records any completed sets. Decide whether short-term quests that say "complete a room" should require full Boss Cleared instead of partial completion.
6. Claimed short-term quests remain visible as completed until the local-date refresh clears the board.
7. Existing refresh is date-based, not an exact countdown timer. The UI copy can say 24H fixed refresh, but the MVP implementation refreshes on calendar date change.

## Recommended Next Steps

1. Manually test save/load in the Godot editor:
   - build a route,
   - complete one stage,
   - restart,
   - continue the next stage,
   - open all rewards,
   - confirm gold cannot be claimed twice.
2. Replace remaining engineering English with Traditional Chinese MVP copy.
3. Add a small save/load diagnostic path or unit-style test.
4. Start curating actual music loop metadata track by track.
5. Begin reward table expansion only after save/load is trusted.
6. Polish Notice Board short-term quest loop:
   - decide full-room vs partial-completion progress rules,
   - add a visible refresh countdown or next-refresh timestamp.
