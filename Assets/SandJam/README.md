# Sand Jam clone – project layout

One playable scene: `Scenes/SandJamGame.unity` (the only scene in the build). `Scenes/LevelStructure.unity`
shows how one level is built (see *Level structure scene* below).

```
Assets/SandJam/
  Scenes/            SandJamGame.unity – loading, home, gameplay, popups, shop, gallery
                     LevelStructure.unity – diagram of one level (editor only, not in the build)
  Scripts/Runtime/   the game
    Core/            rules (SandGame), level data, Burst sand simulation
    Controllers/     gameplay controller, UI screens and popups, boosters
    Managers/        campaign, economy, lives, audio, haptics, save, events
    Views/           board/regions, cubes (characters), effects, UI helpers
  Scripts/Testing/   LevelReplaySolver (finds a winning move order), LevelStructureViewer, AutoPlayer
  Editor/Tests/      editor checks – menu Sand Jam > Tests > Run all checks
  Editor/Tools/      Game Manager window, campaign builder, level authoring, screenshot helper
  Editor/SceneBuilders/  rebuilds SandJamGame from the level catalog (Sand Jam > Levels)
  Levels/Templates/  level templates for new levels (original content, safe to share)
  Prefabs/           GameplayLevelTemplate, PaintRegion, SandCube – starting points for new content
  Generated/         assets produced by the scene builders (LevelPack, Scene3D)
  Resources/         runtime data loaded by name: levels, art, audio (kept local, not in git)
  Fonts/             Lilita One licence
```

## Design patterns

Each pattern lives in its own script under `Scripts/Runtime/Core/Patterns/` (namespace `SandJamTest`), so new
patterns (State, Command, Factory…) can be added next to them as separate files.

- **Observer** – `Core/Patterns/Observer.cs`: `GameEvent`, `GameEvent<T>`, `GameEvent<T1,T2>`, `GameEvent<T1,T2,T3>`.
  Subscribe with `+=`, unsubscribe with `-=`, notify with `Raise(...)`. Raising allocates nothing, a listener may
  unsubscribe while being notified, and an exception in one listener is logged without stopping the others.
  `Managers/GameEvents.cs` declares the game's events (level started/won/lost, region completed, cube
  selected/emptied, sand poured, booster used, coins, button clicked, settings/gallery changed); audio, haptics,
  tasks, stats, effects and UI badges subscribe instead of polling each other. Always unsubscribe in OnDestroy.
  New event: add `public static GameEvent<int> MyEvent = new GameEvent<int>();` plus a `RaiseMyEvent` method.
- **Singleton** – `Core/Patterns/Singleton.cs` (`Singleton<T>`): one live instance via `Instance`, duplicates destroy
  themselves, `Persistent` keeps it across scene loads. Used by `GameServices` (persistent root) and `AudioManager`.
  Subclasses override `OnSingletonAwake` / `OnSingletonDestroy`.
- **Object pooling** – `Core/Patterns/ObjectPooling.cs` (`Pool<T>`, built on Unity's `ObjectPool<T>`): `Get`
  activates an object, `Release`/`ReleaseAt` deactivates it, `Active()` lists the objects in use. Sand pellets
  (`ProjectileManager`) and the region-complete flashes/bubbles (`SandFx`) are reused instead of created and
  destroyed; particles use `ParticleSystem.Emit`. Gameplay ticks allocate nothing (reused lists/arrays) to avoid GC
  spikes on mobile.

## Shared rendering helpers

`Scripts/Runtime/Core/Rendering/` holds what views used to re-implement one by one:

- `SpriteFactory` – cached procedural sprites and materials: `White` / `WhiteBottom` (1-unit square),
  `Rounded(size, radius, pixelsPerUnit)` (9-sliceable Full Rect rounded panel), `Disc`, `SoftDot`,
  `Radial(name, size, alpha, rgb)`, the shared `PlainSprite` material ("Sprites/Default") and
  `CameraForLayer(layer)`. Shared objects are never destroyed or modified by their users (copy the material
  to customise it).
- `RendererExtensions.FitWorldSize(width, height)` – scales a renderer so its drawn bounds have that world size.

New views should use these instead of building their own white / rounded sprites or plain materials.

## Tests

`Sand Jam > Tests > Run all checks` runs every editor check and logs `[Checks] PASS/FAIL` per check:
sand simulation, sand flow, boosters, chained cubes, frozen cubes, level 201 replay, level pack, shaders.
Run it before and after any upgrade. Batch mode: `-executeMethod SandJamTest.Editor.RunAllChecks.Run`
(exits with code 1 on a failure).

## Building a new level

A level is a JSON file (`LevelData`) – the gameplay prefab builds the picture, regions and cube lanes from
it at runtime, so no scene or prefab work is needed for a new level.

1. `Sand Jam > Levels > New level from template` copies `Levels/Templates/Template_Sunset.json` to
   `Resources/Levels/Data/Custom_NNN.json`, appends it to `Resources/Levels/Campaign.json` and makes it the
   next level. Press Play to try it.
2. Edit the JSON (fields below), then run `Sand Jam > Levels > Check custom levels`.

| Field | Meaning |
|---|---|
| `rowCount`, `columnCount` | picture grid (112 x 84) |
| `parts[]` | colour regions: `name`, `ColorType` (1 red, 2 green, 3 blue, 4 yellow, 5 orange, 6 pink, 7 purple, 8 black, 9 white, 10 brown), `rows[]`/`cols[]` cells, `amount` sand needed (multiple of 20, at most the cell count), `isOpenedAtStart`, `prerequests` = neighbouring regions (a region opens once a neighbour is fully covered) |
| `opr[]`, `opc[]` | outline pixels between regions |
| `laneData[3].ColorAmmoDatas[]` | the cube queue of each lane, front first: `ColorType`, `AmmoCount` (400 = a full cube showing 20), flags `IsChain`, `IsSecret`, `IsFreeze`+`FreezeCount`, `IsHalf`, `IsUnlocker` |
| `uiDivider` | sand units per number shown on cubes and regions (20) |
| `gridSlotNeedAmmoCount` | sand to pour before waiting slots 4 and 5 unlock |

### Level structure scene

`Sand Jam > Levels > Open level structure scene` (or open `Scenes/LevelStructure.unity`). Select
**Level Structure** and set `Level Number` (campaign order) or drop any level JSON into `Level Json`.

- Edit mode: a diagram rebuilt on every change, also listed in the Hierarchy under *Level structure (preview)*:
  the board (one pixel per grid cell; bright = open at start, dark = opens later, gold = padlock) with the sand
  of each region, each region's colour / sand / cells / prerequisites, the three lanes front cube first (ammo
  and mechanics: chain, ice, secret, key, half), the five stash slots, and sand vs ammo per colour.
- Play mode: the real level is built from the gameplay template and starts straight away, so the runtime
  objects (regions, characters, board texture, effects) can be inspected in the Hierarchy.

Rules checked by `Check custom levels`: the data is valid, every colour's cubes carry exactly the sand its
regions need, and the level can be won with five waiting slots.

## Prefabs

- `Prefabs/Gameplay/GameplayLevelTemplate` – variant of the runtime gameplay template (board, frame, stash,
  lanes, HUD). Visual changes made here can be applied to the template the game loads.
- `Prefabs/Gameplay/PaintRegion` – one picture region (pour target, counter).
- `Prefabs/Characters/SandCube` – a sand cube with legs, ammo label and effects. Local only (not in git): it is built
  from the original character rig.

These reference art in `Resources/` and `Generated/`, which stay local (original game content).
