# Sand Jam clone – project layout

One playable scene: `Scenes/SandJamGame.unity` (the only scene in the build).

```
Assets/SandJam/
  Scenes/            SandJamGame.unity – loading, home, gameplay, popups, shop, gallery
  Scripts/Runtime/   the game
    Core/            rules (SandGame), level data, Burst sand simulation
    Controllers/     gameplay controller, UI screens and popups, boosters
    Managers/        campaign, economy, lives, audio, haptics, save, events
    Views/           board/regions, cubes (characters), effects, UI helpers
  Scripts/Testing/   LevelReplaySolver (finds a winning move order) and command-line smoke tests
  Editor/Tests/      editor checks – menu Sand Jam > Tests > Run all checks
  Editor/Tools/      Game Manager window, campaign builder, level authoring, screenshot helper
  Editor/SceneBuilders/  rebuilds SandJamGame from the level catalog (Sand Jam > Levels)
  Levels/Templates/  level templates for new levels (original content, safe to share)
  Prefabs/           GameplayLevelTemplate, PaintRegion, SandCube – starting points for new content
  Generated/         assets produced by the scene builders (LevelPack, Scene3D)
  Resources/         runtime data loaded by name: levels, art, audio (kept local, not in git)
  Fonts/             Lilita One licence
```

## Tests

`Sand Jam > Tests > Run all checks` runs every editor check and logs `[Checks] PASS/FAIL` per check:
sand simulation, sand flow, boosters, chained cubes, frozen cubes, level 201 replay, level pack, shaders.
Run it before and after any upgrade. Batch mode: `-executeMethod SandJamTest.Editor.RunAllChecks.Run`
(exits with code 1 on a failure).

Smoke tests (`Scripts/Testing/Smoke`) play the game automatically in a built player:
`--booster-smoke`, `--feeling-smoke`, `--video-smoke <folder>`.

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

Rules checked by `Check custom levels`: the data is valid, every colour's cubes carry exactly the sand its
regions need, and the level can be won with five waiting slots.

## Prefabs

- `Prefabs/Gameplay/GameplayLevelTemplate` – variant of the runtime gameplay template (board, frame, stash,
  lanes, HUD). Visual changes made here can be applied to the template the game loads.
- `Prefabs/Gameplay/PaintRegion` – one picture region (pour target, counter).
- `Prefabs/Characters/SandCube` – a sand cube with legs, ammo label and effects.

These reference art in `Resources/` and `Generated/`, which stay local (original game content).
