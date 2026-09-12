using System.Globalization;

namespace UniTracks.Services.Changelog;

/// <summary>
/// Compares dotted version strings ("0.4", "0.3.1", "0.4.0") numerically so that the changelog can
/// work out which releases are newer than the version the user last saw.
/// </summary>
public static class ChangelogVersion
{
    /// <summary>Returns a negative value when <paramref name="left"/> is older, 0 when equal, positive when newer.</summary>
    public static int Compare(string? left, string? right)
    {
        var leftParts = Split(left);
        var rightParts = Split(right);
        var length = Math.Max(leftParts.Length, rightParts.Length);

        for (var index = 0; index < length; index++)
        {
            var leftPart = index < leftParts.Length ? leftParts[index] : 0;
            var rightPart = index < rightParts.Length ? rightParts[index] : 0;

            if (leftPart != rightPart)
            {
                return leftPart.CompareTo(rightPart);
            }
        }

        return 0;
    }

    /// <summary>True when both strings describe the same version, ignoring trailing zero segments.</summary>
    public static bool IsSame(string? left, string? right) => Compare(left, right) == 0;

    /// <summary>True when the string contains at least one numeric segment.</summary>
    public static bool IsValid(string? version) => Split(version).Length > 0;

    private static int[] Split(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return [];
        }

        var rawParts = version.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var parts = new List<int>(rawParts.Length);

        foreach (var rawPart in rawParts)
        {
            // Take only the leading digits so that suffixes such as "0.4-beta1" still compare as 0.4.
            var digits = 0;
            while (digits < rawPart.Length && char.IsAsciiDigit(rawPart[digits]))
            {
                digits++;
            }

            if (digits == 0)
            {
                continue;
            }

            parts.Add(int.TryParse(rawPart[..digits], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0);
        }

        return [.. parts];
    }
}
