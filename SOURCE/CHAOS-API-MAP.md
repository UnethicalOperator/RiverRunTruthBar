# Current Chaos API map

The current Steam game is Unity IL2CPP. Its original `ChaosGamePlay` implementation is compiled into native code, not stored as an editable C# file with stable source lines.

Native implementation:

`C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar\GameAssembly.dll`

Generated managed wrapper used to build RiverRunTruthBar:

`C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar\BepInEx\interop\Assembly-CSharp.dll`

Type: `ChaosGamePlay`

Current aim/fire members confirmed from that generated assembly:

- `GetAim(Int32)`
- `LeftAim()`
- `RightAim()`
- `GetFirstAim()`
- `LockTheAim(NetworkConnectionToClient)`
- `NetworkTakingAim`
- `NetworkAim`
- `NetworkAimLocked`
- `TryFire()`
- `HitTargetCmd()`
- `SetDeadAnim(NetworkConnectionToClient, GameObject)`
- `WaitforFire()`

There is no truthful C# line number inside these DLLs. Any claimed line would be fabricated and would change when the game regenerates its IL2CPP wrappers.

RiverRunTruthBar's editable source file is `src\TruthBar.IL2CPP\TruthBarBehaviour.cs`. Version 2.2 reads Chaos hand objects at lines 201-208. It does not invoke the aim/fire members because the current path is coupled to network targeting, hit processing and death state rather than a standalone harmless animation.
