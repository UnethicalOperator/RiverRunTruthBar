using System.Globalization;
using System.Text;

namespace TruthBar;

public static class TruthBarLogic
{
    public static readonly string[] RankNames =
    {
        "Truth Teller",
        "Rookie",
        "Trickster",
        "Hustler",
        "Con Artist",
        "Silver Tongued Devil",
        "Master of Deception"
    };

    public static bool TryNormalizeBullet(string? input, out int value, out string normalized)
    {
        var digits = new StringBuilder();
        if (input is not null)
        {
            foreach (var character in input)
            {
                if (char.IsDigit(character))
                {
                    digits.Append(character);
                }
            }
        }

        if (digits.Length == 0 || !int.TryParse(digits.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            value = 0;
            normalized = "0";
            return false;
        }

        value = Math.Clamp(value, 0, 999);
        normalized = value.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    public static string CardLabel(int cardType) => cardType switch
    {
        -1 => "DEVIL",
        1 => "K",
        2 => "Q",
        3 => "A",
        4 => "J",
        _ => cardType.ToString(CultureInfo.InvariantCulture)
    };

    public static string ColoredCardLabel(int cardType) => cardType switch
    {
        -1 => "<color=red>DEVIL</color>",
        1 => "<color=red>K</color>",
        2 => "<color=purple>Q</color>",
        3 => "<color=orange>A</color>",
        4 => "<color=white>J</color>",
        _ => CardLabel(cardType)
    };

    public static string DiceLabel(int face) => face switch
    {
        >= 1 and <= 6 => $"die {face}",
        0 => "not rolled",
        _ => $"raw {face}"
    };

    public static string TexasCardLabel(int cardNumber, int cardValue, string suit)
    {
        var safeSuit = string.IsNullOrWhiteSpace(suit) ? "unknown suit" : suit;
        return $"{safeSuit} #{cardNumber} (value {cardValue})";
    }

    public static string DeathTimer(int currentChamber, int lethalChamber)
    {
        return lethalChamber - currentChamber == 0
            ? "Dead on next!"
            : $"{currentChamber} / {lethalChamber + 1}";
    }

    public static string RankName(int rank)
    {
        return rank >= 0 && rank < RankNames.Length ? RankNames[rank] : $"Unknown ({rank})";
    }
}
