# Third-party notices

## BepInEx

The one-click launcher embeds the official `BepInEx-Unity.IL2CPP-win-x64-6.0.0-pre.2.zip` release asset.

- Project: https://github.com/BepInEx/BepInEx
- Release archive SHA-256: `616EC7EB06CF11B2A0000E8FCEF04D1B12BB58E84A2E0BDAC9523234FC193CEB`
- License copy: `vendor/BepInEx-LICENSE.txt`

RiverRunTruthBar verifies the embedded archive hash before extracting it and rejects archive entries that would escape the game directory.

## Legacy Liars-Bar-Bot project

The preserved `Main` and `Sources` directories originate from the historical `ca1ik/Liars-Bar-Bot` project. They are retained as legacy reference material and are not loaded by RiverRunTruthBar's current IL2CPP launcher.

- Project: https://github.com/ca1ik/Liars-Bar-Bot
