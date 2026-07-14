# RiverRunTruthBar 2.2 editing map

Open `src\TruthBar.IL2CPP\TruthBarBehaviour.cs` to add or change in-game menu pages, overlay text, live player reads, buttons, self controls, rank Apply behavior, and game actions.

Current 2.2 locations in that file:

- Lines 183-254: collect each detected player's live state.
- Lines 201-226: select real Chaos Deck, Chaos, Poker, Deck, Liar's Deck, Texas and Dice hand data.
- Lines 290-331: read standard card objects and Texas hand objects.
- Lines 333-371: read `DiceValues`, with raw rendered `Dice.Face` indexes as the no-guess fallback.
- Lines 469-479: default-on **Always reveal hands and dice** control.
- Lines 662-683: two-line always-visible player hand/dice overlay.
- Lines 715-751: hand and dice display formatting; both helpers are excluded from IL2CPP native registration.
- Lines 1071 onward: `PlayerView` snapshot fields carried into the overlay.

Supporting files:

- `src\TruthBar.IL2CPP\TruthBarLogic.cs`: card, dice, Texas-card, rank and chamber labels.
- `src\TruthBar.IL2CPP\Plugin.cs`: plug-in title/version/startup and runtime markers.
- `src\TruthBar.Launcher\LauncherService.cs`: verified loader installation, plug-in installation, backups and Steam launch.
- `src\TruthBar.Launcher\MainForm.cs`: launcher window and creator credit.
- `tests\TruthBar.Tests\Program.cs`: automated logic and installer-safety checks.
- `scripts\build.ps1`: complete release build and self-contained EXE publication.
- `CHAOS-API-MAP.md`: exact current Chaos binary/type/member map and why it has no honest C# source line number.

## Rebuild

Close Liar's Bar, open PowerShell in this source folder, and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

The rebuilt `RiverRunTruthBar.exe` appears at the source-folder root. Double-click it to install the new plug-in and start the game through Steam.

Line numbers describe the packaged 2.2 source. Recalculate them after adding or removing lines.
