# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

AQUA-MELODIA — a 2D pixel-art fishing game in **Unity 6000.3.12f1** (URP 2D). All gameplay code lives in `Assets/Scripts`; code comments, logs, and commit messages are in Korean. Commit style: `[Add] ...`, `[Fix] ...`.

There is no CLI build, lint, or test setup — open the project in the Unity Editor (Rider/VS for C#). No test assemblies or `.asmdef` files exist; everything compiles into `Assembly-CSharp`. Build scenes (in order): `Ocean`, `Aquarium`, `SelectMap`, `Home`. `TestFish.unity` is a sandbox scene.

Key packages (git UPM deps in `Packages/manifest.json`): **R3** (reactive properties), **UniTask** (async), **WaterRW** (water rendering), NuGetForUnity.

## Architecture

### Fishing state machine (`FishingSystem/FishState`)
`FishingRod` (MonoBehaviour) is the hub: it owns a plain-C# `FishingStateMachine` and one instance of every `FishingState`, created in `Awake`. Flow:

`Ready → Casting → Settled → Biting → MiniGame → FinalStruggle → Caught → Showcase`, with `Retrieving` (reel in early) and `Failed` branches back to `Ready`.

- Each state's constructor takes an animator bool name; `Enter`/`Exit` toggle it. Animation clips drive timing via animation events (`OnAnimationEvent_ThrowBobber`, `_PullBobber`, `_FailFinished`, `_ShowcaseFish`) relayed through `FishingAnimationEventRelay` — these check the current state before acting.
- Timed/async behavior in states uses `UniTaskVoid` + a `CancellationTokenSource` created in `Enter` and cancelled/disposed in `Exit`. Follow this pattern so state changes cancel pending work.
- Tunable gameplay values are public inspector fields on `FishingRod`; equipment bonuses come from `PlayerFishingEquipment` / `FishingRodDataSO` via the `Effective*` properties.
- Mouse input for fishing goes through `Input_Helper/FishingInput` (legacy `Input`), which ignores clicks over UI and colliders tagged `Interactive`. The pattern-drawing minigame uses the new Input System (`Mouse.current`) directly.

### UI: Model–View–Presenter with R3
`FishingRod` exposes `ReactiveProperty<T>` fields (cast power, reel ratio, line stress, fish HP, struggle ink, showcase fish, …). Each UI folder under `UI/` has a `*UIPresenter` that subscribes to those properties (`.Subscribe(...).AddTo(this)`) and pushes values into a passive `*UIView`. New UI should add a reactive property on the model and a presenter/view pair rather than having states touch UI directly. Reactive properties are disposed in `FishingRod.OnDestroy` — add new ones there.

### Fish data and spawning
- `FishDataSO` (ScriptableObject) = species definition; `FishData` = a caught instance (length, quality, stamina). `FishData.Equals` compares by species SO, so it's used as a per-species dictionary key (encyclopedia/records).
- `FishingZone` holds grade probabilities and a weighted spawn list. `GetRandomFish()` rolls grade first, then weighted species, with the equipped bait (`BaitManager` / `BaitDataSO`) temporarily adding `Stat` modifiers that are reverted after the roll. The zone is found by an overlap circle around the bobber when it settles.
- Escape (final struggle) patterns: `PatternDataSO` / `EscapePatternDataSO` → `PatternGenerator` (spawns pooled dots/detectors) → `PatternDrawer` (player draws) → `PatternEvaluator`.

### Persistent singletons
Cross-scene state uses `static Instance` + `DontDestroyOnLoad` managers: `FishingDataManager` (caught-fish inventory, encyclopedia, records; R3 `Subject` events), `HouseDataManager` (aquarium), `IslandManager` (island unlocks, gold), `BaitManager`, `WorldTime`, `LoadingManager` (async scene loads with `WipeController` transition). Scene-local singletons: `CameraManager`, `FishChest`, `PlayerFishingEquipment`, `IslandUIController`. All progress is in-memory only — there is no save system yet.

### Editor scripts
Custom inspectors (`*Editor.cs`) sit next to their runtime classes in `Assets/Scripts` rather than in an `Editor/` folder; they must stay wrapped in `#if UNITY_EDITOR` or they will break player builds.
