using System.Globalization;

namespace UniTracks.Services.Text;

/// <summary>
/// Formats a trip's start for the trip list: "Heute, 07:32" / "Gestern, 18:05" for the last two
/// days, the absolute date beyond that. Relative labels are quicker to scan when the list is
/// dominated by recent trips. The timestamp is converted to the device's local time first — trips
/// are stored in UTC.
/// </summary>
public static class RelativeDateFormatter
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static string Format(DateTimeOffset timestamp) => Format(timestamp, DateTimeOffset.Now);

    public static string Format(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var local = timestamp.ToLocalTime();
        var localNow = now.ToLocalTime();

        if (local.Date == localNow.Date)
        {
            return local.ToString("'Heute', HH:mm", GermanCulture);
        }

        if (local.Date == localNow.Date.AddDays(-1))
        {
            return local.ToString("'Gestern', HH:mm", GermanCulture);
        }

        return local.ToString("ddd, dd. MMM · HH:mm", GermanCulture);
    }
}
