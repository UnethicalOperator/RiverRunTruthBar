# RiverRunTruthBar

**RiverRunTruthBar 2.2 — created by PlsDntChase / RiverRunCartel**

> **Source available — proprietary — All Rights Reserved.** This is not an open-source project. The official unmodified executable may be used personally and non-commercially; modification, derivative releases, redistribution and commercial use are prohibited without prior written permission. See [`LICENSE.md`](LICENSE.md).

RiverRunTruthBar is a one-click IL2CPP overlay for the current Steam version of Liar's Bar. It replaces the obsolete Mono-injector workflow used by the legacy project.

## Run it

1. Close Liar's Bar if it is already running.
2. Double-click `RiverRunTruthBar.exe` in this folder.
3. The launcher finds Steam and Liar's Bar, installs or updates the bundled verified loader and mod, verifies the installed files, and starts the game through Steam.
4. Press `F1` in game to show or hide the right-side menu.

No separate injector, namespace, class name, or method name is required.

## Hotkeys

- `F1`: show or hide the RiverRunTruthBar menu.
- `F2`: close the menu and reset overlay positions.
- `L`: refresh live lobby or match state.
- `P`: reset overlay positions and return to the Main page.

## Menu pages and features

### Main

- Move your local lethal revolver chamber to a chosen value.
- Quick actions for the next chamber or chamber six.
- Show or hide the player-card and table-card overlays independently.
- Keep the default-on live hand-and-dice reveal enabled or hide it temporarily.
- See the declared round card and current table-card counts.
- See the current Dice bid, face, total dice and bidder when Dice mode exposes its manager.

### Players

- Select any currently detected player.
- See health, alive/dead state, current-turn state, cards, and revolver prediction.
- Continuously read real hand objects across Liar's Deck, Chaos Deck, Chaos, Poker, Deck and Texas components.
- Continuously read each detected player's real `DiceValues`; when only rendered dice are present, show their raw `Dice.Face` indexes explicitly instead of guessing a face value.
- Preserve the original card tools: set the selected player's visible card objects to Kings, Queens, Aces, or Jokers.

### Self

- Move your own head look left, right, up, or down through the game's current local `ArenaGameplay` controller.
- Enable a wider local head-look range.
- Reset the exact captured angles and limits.
- Automatic best-effort rollback when the plugin is destroyed or the game closes normally.

### Rank

- See the currently selected rank.
- Choose any of the seven current rank values, through Master of Deception, as a pending selection.
- Press **Apply selected rank** to call the game's current profile-rank method.
- Choose Truth Teller as the reset selection, then press Apply.
- This controls the game's selected profile rank. Server-authoritative XP and leaderboard progression may validate or revert it; RiverRunTruthBar does not forge leaderboard records.

### Intel

- Declared round card, reported cards on the table, and last revealed group count.
- Seat, name, local-player marker, health, alive/dead state, turn state, card count, and revolver death prediction for every detected player.
- Per-player hand and dice counts/values from the current live gameplay objects.

## Safety and compatibility notes

- The current Steam game is 64-bit Unity IL2CPP. `Main\ub0.dll` and the old SharpMonoInjector instructions target the earlier Mono build and are retained only as legacy reference material. Do not inject that DLL into the current game.
- The launcher embeds the official BepInEx Unity IL2CPP x64 6.0.0-pre.2 archive and verifies its SHA-256 before installation.
- Changed loader/config/plugin files are backed up under `BepInEx\TruthBarBackups`, outside the plugin scan directory.
- An unrestricted remote-player force-death button is not included. The current game exposes a network death command but no dependable private-host-only classification gate, so presenting it as a safe private-lobby action would be misleading.
- The current Chaos aim/fire path is inside the generated `ChaosGamePlay` wrapper and is coupled to `TryFire`, `HitTargetCmd`, `SetDeadAnim`, and network state. It is documented for source navigation but not relabelled as a harmless animation.
- Hidden-hand and dice values are displayed only when the current client actually exposes them in live gameplay objects. The overlay says `not exposed yet` rather than inventing a value.
- This is an unofficial local mod. Game updates can change IL2CPP APIs and may require a rebuild.

## Build from source

Requirements: Windows x64, .NET SDK 10.0.301 or a compatible patch, the installed current Steam game (for generated IL2CPP reference assemblies), and the vendored BepInEx archive.

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The script builds the plugin with warnings treated as errors, runs the tests, publishes the self-contained launcher, verifies the staged hash, and writes `RiverRunTruthBar.exe` at the repository root.

## Credits

- Product title and creator credit: **PlsDntChase / RiverRunCartel**.
- Current IL2CPP port, one-click launcher, safety checks, tests, and 2.2 feature pass: built for the local RiverRunTruthBar project.
- Legacy project lineage: `ca1ik/Liars-Bar-Bot`.
- Loader: BepInEx contributors. See `THIRD-PARTY-NOTICES.md` and `vendor\BepInEx-LICENSE.txt`.

## License and ownership

Copyright © 2026 **PlsDntChase / RiverRunCartel**. All rights reserved.

- The original RiverRunTruthBar material is proprietary and remains owned by its applicable rights holder.
- Recipients may run an unmodified official executable for personal, non-commercial use.
- No permission is granted to edit, patch, port, publish, redistribute, sell, sublicense or release derivative versions.
- Contributions and pull requests are closed unless PlsDntChase / RiverRunCartel gives prior written approval.
- Liar's Bar, BepInEx and legacy/upstream material remain the property of their respective owners and retain their own license terms.

See [`LICENSE.md`](LICENSE.md) for the complete terms and [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for excluded third-party material.

### Important GitHub limitation

Publishing the repository publicly on GitHub permits other GitHub users to view and fork it through GitHub's service under GitHub's Terms. If preventing public access and forks is essential, keep the repository **private**. Public visibility does not grant a broader open-source license, but it cannot truthfully be described as “no copies or forks.”
