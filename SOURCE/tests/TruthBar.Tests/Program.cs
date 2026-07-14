using TruthBar;
using TruthBar.Launcher;

var tests = new List<(string Name, Action Test)>
{
    ("empty bullet input fails safely", () => ExpectNormalize("", false, 0, "0")),
    ("letters without digits fail safely", () => ExpectNormalize("nope", false, 0, "0")),
    ("digits are retained", () => ExpectNormalize("x1y2", true, 12, "12")),
    ("bullet input is clamped", () => ExpectNormalize("12345", true, 999, "999")),
    ("king card mapping", () => Equal("K", TruthBarLogic.CardLabel(1))),
    ("queen card mapping", () => Equal("Q", TruthBarLogic.CardLabel(2))),
    ("ace card mapping", () => Equal("A", TruthBarLogic.CardLabel(3))),
    ("joker card mapping", () => Equal("J", TruthBarLogic.CardLabel(4))),
    ("devil card mapping", () => Equal("DEVIL", TruthBarLogic.CardLabel(-1))),
    ("unknown card mapping", () => Equal("9", TruthBarLogic.CardLabel(9))),
    ("king rich text", () => Equal("<color=red>K</color>", TruthBarLogic.ColoredCardLabel(1))),
    ("dice face label", () => Equal("die 6", TruthBarLogic.DiceLabel(6))),
    ("unrolled dice label", () => Equal("not rolled", TruthBarLogic.DiceLabel(0))),
    ("raw dice label", () => Equal("raw 9", TruthBarLogic.DiceLabel(9))),
    ("Texas card uses exact exposed fields", () => Equal("Hearts #12 (value 10)", TruthBarLogic.TexasCardLabel(12, 10, "Hearts"))),
    ("death-next prediction", () => Equal("Dead on next!", TruthBarLogic.DeathTimer(3, 3))),
    ("death timer fraction", () => Equal("2 / 6", TruthBarLogic.DeathTimer(2, 5))),
    ("all original ranks retained", () => Equal(7, TruthBarLogic.RankNames.Length)),
    ("top rank retained", () => Equal("Master of Deception", TruthBarLogic.RankNames[6])),
    ("rank display mapping", () => Equal("Rookie", TruthBarLogic.RankName(1))),
    ("unknown rank display", () => Equal("Unknown (99)", TruthBarLogic.RankName(99))),
    ("safe payload path remains under root", () => SafePathStaysUnderRoot()),
    ("payload traversal is rejected", () => PayloadTraversalIsRejected()),
    ("Steam libraries are parsed", () => SteamLibrariesAreParsed()),
    ("existing INI value is replaced", () => ExistingIniValueIsReplaced()),
    ("missing INI section is added", () => MissingIniSectionIsAdded())
};

var failed = 0;
foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

Console.WriteLine($"{tests.Count - failed}/{tests.Count} tests passed.");
return failed == 0 ? 0 : 1;

static void ExpectNormalize(string input, bool expectedSuccess, int expectedValue, string expectedText)
{
    var success = TruthBarLogic.TryNormalizeBullet(input, out var value, out var normalized);
    Equal(expectedSuccess, success);
    Equal(expectedValue, value);
    Equal(expectedText, normalized);
}

static void Equal<T>(T expected, T actual)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}, got {actual}");
    }
}

static void SafePathStaysUnderRoot()
{
    var root = Path.Combine(Path.GetTempPath(), "TruthBarTestRoot");
    var actual = LauncherLogic.GetSafeDestination(root, Path.Combine("BepInEx", "core", "test.dll"));
    Equal(Path.GetFullPath(Path.Combine(root, "BepInEx", "core", "test.dll")), actual);
}

static void PayloadTraversalIsRejected()
{
    var root = Path.Combine(Path.GetTempPath(), "TruthBarTestRoot");
    try
    {
        _ = LauncherLogic.GetSafeDestination(root, Path.Combine("..", "escaped.dll"));
    }
    catch (InvalidDataException)
    {
        return;
    }

    throw new InvalidOperationException("traversal path was accepted");
}

static void SteamLibrariesAreParsed()
{
    var vdf = "\"path\"  \"D:\\\\SteamLibrary\"\n\"path\" \"E:\\\\Games\"";
    var paths = LauncherLogic.ParseSteamLibraryPaths(vdf);
    Equal(2, paths.Count);
    Equal(@"D:\SteamLibrary", paths[0]);
}

static void ExistingIniValueIsReplaced()
{
    var changed = LauncherLogic.SetIniValue("[Logging.Console]\nEnabled = true\n", "Logging.Console", "Enabled", "false");
    if (!changed.Contains("Enabled = false", StringComparison.Ordinal) || changed.Contains("Enabled = true", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("existing value was not replaced");
    }
}

static void MissingIniSectionIsAdded()
{
    var changed = LauncherLogic.SetIniValue(string.Empty, "Logging.Disk", "Enabled", "true");
    if (!changed.Contains("[Logging.Disk]", StringComparison.Ordinal) || !changed.Contains("Enabled = true", StringComparison.Ordinal))
    {
        throw new InvalidOperationException("missing section was not added");
    }
}
