using System.Globalization;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace TruthBar;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "riverruncartel.liarsbar.truthbar";
    public const string PluginName = "RiverRunTruthBar";
    public const string PluginVersion = "2.2.0";

    internal static ManualLogSource PluginLog { get; private set; } = null!;

    public override void Load()
    {
        PluginLog = Log;
        Log.LogInfo($"Loading {PluginName} {PluginVersion} for the current IL2CPP runtime.");
        WriteRuntimeMarker("PluginLoad", "BepInEx accepted the plugin assembly");
        AddComponent<TruthBarBehaviour>();
    }

    internal static void WriteRuntimeMarker(string stage, string detail)
    {
        try
        {
            var path = Path.Combine(Paths.BepInExRootPath, "TruthBar.runtime.log");
            var line = string.Join(
                " | ",
                DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                PluginVersion,
                stage.Replace('|', '/'),
                detail.Replace('|', '/'));
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            PluginLog?.LogWarning($"Could not write the runtime marker: {ex.Message}");
        }
    }
}
