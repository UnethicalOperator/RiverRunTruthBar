using System.Text;
using System.Text.RegularExpressions;

namespace TruthBar.Launcher;

public static partial class LauncherLogic
{
    public static string GetSafeDestination(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        var rootPrefix = fullRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Archive entry escapes the game directory: {relativePath}");
        }

        return candidate;
    }

    public static IReadOnlyList<string> ParseSteamLibraryPaths(string? vdf)
    {
        if (string.IsNullOrWhiteSpace(vdf))
        {
            return Array.Empty<string>();
        }

        return SteamPathRegex()
            .Matches(vdf)
            .Select(match => match.Groups[1].Value.Replace("\\\\", "\\", StringComparison.Ordinal))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string SetIniValue(string text, string section, string key, string value)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n').ToList();
        var sectionHeader = $"[{section}]";
        var sectionIndex = lines.FindIndex(line => string.Equals(line.Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase));

        if (sectionIndex < 0)
        {
            if (lines.Count > 0 && lines[^1].Length != 0)
            {
                lines.Add(string.Empty);
            }

            lines.Add(sectionHeader);
            lines.Add($"{key} = {value}");
            return string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine;
        }

        var nextSection = lines.FindIndex(sectionIndex + 1, line => line.TrimStart().StartsWith("[", StringComparison.Ordinal));
        if (nextSection < 0)
        {
            nextSection = lines.Count;
        }

        for (var index = sectionIndex + 1; index < nextSection; index++)
        {
            var equalsIndex = lines[index].IndexOf('=');
            if (equalsIndex < 0 || !string.Equals(lines[index][..equalsIndex].Trim(), key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lines[index] = $"{key} = {value}";
            return string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine;
        }

        lines.Insert(nextSection, $"{key} = {value}");
        return string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine;
    }

    [GeneratedRegex("\\\"path\\\"\\s+\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SteamPathRegex();
}
