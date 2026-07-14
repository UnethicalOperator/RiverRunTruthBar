using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace TruthBar.Launcher;

internal static partial class LauncherService
{
    private const string BepInExResource = "TruthBar.Payload.BepInEx.zip";
    private const string PluginResource = "TruthBar.Payload.TruthBar.IL2CPP.dll";
    private const string ExpectedBepInExHash = "616EC7EB06CF11B2A0000E8FCEF04D1B12BB58E84A2E0BDAC9523234FC193CEB";
    private const string PluginVersion = "2.2.0";
    private const int SteamAppId = 3097560;

    public static InstallResult InstallAndLaunch(IProgress<string> progress)
    {
        progress.Report("Finding the installed Steam game…");
        var gameRoot = FindGameDirectory() ?? throw new DirectoryNotFoundException(
            "Liar's Bar was not found in the Steam libraries. Install app 3097560 first.");
        ValidateGame(gameRoot);

        if (IsGameRunning())
        {
            throw new InvalidOperationException("Liar's Bar is already running. Close it, then press Retry.");
        }

        progress.Report($"Found Liar's Bar at {gameRoot}");
        var backupRoot = Path.Combine(
            gameRoot,
            "BepInEx",
            "TruthBarBackups",
            DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture));
        var counters = new InstallCounters();

        progress.Report("Installing the verified IL2CPP loader…");
        var archiveBytes = LoadEmbeddedBytes(BepInExResource);
        var archiveHash = Convert.ToHexString(SHA256.HashData(archiveBytes));
        if (!string.Equals(archiveHash, ExpectedBepInExHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The embedded BepInEx archive failed its SHA-256 integrity check.");
        }

        using (var archiveStream = new MemoryStream(archiveBytes, writable: false))
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                var destination = LauncherLogic.GetSafeDestination(gameRoot, relativePath);
                using var entryStream = entry.Open();
                using var buffer = new MemoryStream();
                entryStream.CopyTo(buffer);
                WriteFileIfDifferent(destination, buffer.ToArray(), gameRoot, backupRoot, counters);
            }
        }

        progress.Report("Installing RiverRunTruthBar 2.2…");
        var pluginBytes = LoadEmbeddedBytes(PluginResource);
        var pluginDestination = Path.Combine(gameRoot, "BepInEx", "plugins", "TruthBar", "TruthBar.IL2CPP.dll");
        WriteFileIfDifferent(pluginDestination, pluginBytes, gameRoot, backupRoot, counters);
        var pluginHash = Convert.ToHexString(SHA256.HashData(pluginBytes));

        ConfigureBepInEx(gameRoot, backupRoot, counters);
        WriteInstallManifest(gameRoot, pluginHash, archiveHash, counters);
        VerifyInstalledFiles(gameRoot, pluginHash);

        progress.Report("Everything is installed and verified. Launching through Steam…");
        using var launched = Process.Start(new ProcessStartInfo($"steam://rungameid/{SteamAppId}")
        {
            UseShellExecute = true
        });
        if (launched is null)
        {
            throw new InvalidOperationException("Windows could not hand the launch request to Steam.");
        }

        return new InstallResult(gameRoot, counters.Written, counters.Unchanged, counters.BackedUp, pluginHash);
    }

    private static string? FindGameDirectory()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar"
        };
        var steamRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRegistrySteamRoot(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath", steamRoots);
        AddRegistrySteamRoot(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", steamRoots);

        foreach (var steamRoot in steamRoots.ToArray())
        {
            var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFile))
            {
                foreach (var library in LauncherLogic.ParseSteamLibraryPaths(File.ReadAllText(libraryFile)))
                {
                    steamRoots.Add(library);
                }
            }
        }

        foreach (var libraryRoot in steamRoots)
        {
            var steamApps = Path.Combine(libraryRoot, "steamapps");
            var manifest = Path.Combine(steamApps, $"appmanifest_{SteamAppId}.acf");
            if (!File.Exists(manifest))
            {
                continue;
            }

            var match = InstallDirRegex().Match(File.ReadAllText(manifest));
            if (match.Success)
            {
                candidates.Add(Path.Combine(steamApps, "common", match.Groups[1].Value));
            }
        }

        return candidates.FirstOrDefault(IsValidGameDirectory);
    }

    private static void AddRegistrySteamRoot(
        RegistryKey hive,
        string subKey,
        string valueName,
        ISet<string> steamRoots)
    {
        try
        {
            using var key = hive.OpenSubKey(subKey, writable: false);
            if (key?.GetValue(valueName) is string value && Directory.Exists(value))
            {
                steamRoots.Add(value.Replace('/', Path.DirectorySeparatorChar));
            }
        }
        catch (Exception)
        {
            // The known-path and manifest scans remain available if a registry hive is inaccessible.
        }
    }

    private static bool IsValidGameDirectory(string path)
    {
        return File.Exists(Path.Combine(path, "Liar's Bar.exe"))
            && File.Exists(Path.Combine(path, "GameAssembly.dll"))
            && File.Exists(Path.Combine(path, "Liar's Bar_Data", "il2cpp_data", "Metadata", "global-metadata.dat"));
    }

    private static void ValidateGame(string gameRoot)
    {
        if (!IsValidGameDirectory(gameRoot))
        {
            throw new InvalidDataException("The detected folder is not the supported 64-bit IL2CPP game layout.");
        }
    }

    private static bool IsGameRunning()
    {
        var processes = Process.GetProcessesByName("Liar's Bar");
        try
        {
            return processes.Length != 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static byte[] LoadEmbeddedBytes(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded payload is missing: {resourceName}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void WriteFileIfDifferent(
        string destination,
        byte[] content,
        string gameRoot,
        string backupRoot,
        InstallCounters counters)
    {
        if (File.Exists(destination))
        {
            var currentHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(destination)));
            var newHash = Convert.ToHexString(SHA256.HashData(content));
            if (string.Equals(currentHash, newHash, StringComparison.Ordinal))
            {
                counters.Unchanged++;
                return;
            }

            var relative = Path.GetRelativePath(gameRoot, destination);
            var backup = LauncherLogic.GetSafeDestination(backupRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            File.Copy(destination, backup, overwrite: false);
            counters.BackedUp++;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + ".truthbar.tmp";
        try
        {
            File.WriteAllBytes(temporary, content);
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }

        counters.Written++;
    }

    private static void ConfigureBepInEx(string gameRoot, string backupRoot, InstallCounters counters)
    {
        var configPath = Path.Combine(gameRoot, "BepInEx", "config", "BepInEx.cfg");
        var text = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
        text = LauncherLogic.SetIniValue(text, "Logging.Console", "Enabled", "false");
        text = LauncherLogic.SetIniValue(text, "Logging.Disk", "Enabled", "true");
        text = LauncherLogic.SetIniValue(text, "Logging.Disk", "WriteUnityLog", "false");
        text = LauncherLogic.SetIniValue(text, "Logging.Disk", "AppendLog", "false");
        WriteFileIfDifferent(
            configPath,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(text),
            gameRoot,
            backupRoot,
            counters);
    }

    private static void WriteInstallManifest(
        string gameRoot,
        string pluginHash,
        string archiveHash,
        InstallCounters counters)
    {
        var manifestPath = Path.Combine(gameRoot, "BepInEx", "plugins", "TruthBar", "install-manifest.txt");
        var contents = string.Join(
            Environment.NewLine,
            "Product=RiverRunTruthBar",
            "CreatedBy=PlsDntChase / RiverRunCartel",
            $"TruthBarVersion={PluginVersion}",
            $"InstalledAt={DateTimeOffset.Now:O}",
            $"PluginSHA256={pluginHash}",
            $"BepInExArchiveSHA256={archiveHash}",
            $"Written={counters.Written}",
            $"Unchanged={counters.Unchanged}",
            $"BackedUp={counters.BackedUp}",
            string.Empty);
        File.WriteAllText(manifestPath, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void VerifyInstalledFiles(string gameRoot, string expectedPluginHash)
    {
        var required = new[]
        {
            Path.Combine(gameRoot, "winhttp.dll"),
            Path.Combine(gameRoot, "doorstop_config.ini"),
            Path.Combine(gameRoot, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll"),
            Path.Combine(gameRoot, "BepInEx", "plugins", "TruthBar", "TruthBar.IL2CPP.dll")
        };
        var missing = required.Where(path => !File.Exists(path)).ToArray();
        if (missing.Length != 0)
        {
            throw new IOException("Install verification failed; missing: " + string.Join(", ", missing));
        }

        var installedPluginHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(required[^1])));
        if (!string.Equals(installedPluginHash, expectedPluginHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Installed TruthBar plugin hash does not match the embedded build.");
        }
    }

    [GeneratedRegex("\\\"installdir\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InstallDirRegex();

    private sealed class InstallCounters
    {
        public int Written { get; set; }

        public int Unchanged { get; set; }

        public int BackedUp { get; set; }
    }
}

internal sealed record InstallResult(
    string GameRoot,
    int Written,
    int Unchanged,
    int BackedUp,
    string PluginHash);
