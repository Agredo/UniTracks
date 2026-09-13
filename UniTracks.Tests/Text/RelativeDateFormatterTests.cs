using UniTracks.Services.Text;

namespace UniTracks.Tests.Text;

/// <summary>
/// Guards the relative date labels of the trip list: today/yesterday get a relative label, older
/// trips the absolute date. The timestamps are stored in UTC, so the formatter must convert to the
/// device's local time before comparing calendar days.
/// </summary>
public sealed class RelativeDateFormatterTests
{
    [Fact]
    public void SameDay_IsToday()
    {
        var trip = new DateTimeOffset(2024, 6, 10, 7, 32, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2024, 6, 10, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal($"Heute, {trip.ToLocalTime():HH:mm}", RelativeDateFormatter.Format(trip, now));
    }

    [Fact]
    public void PreviousDay_IsYesterday()
    {
        var trip = new DateTimeOffset(2024, 6, 9, 18, 5, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2024, 6, 10, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal($"Gestern, {trip.ToLocalTime():HH:mm}", RelativeDateFormatter.Format(trip, now));
    }

    [Fact]
    public void OlderTrip_ShowsTheAbsoluteDate()
    {
        var trip = new DateTimeOffset(2024, 6, 3, 9, 15, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2024, 6, 10, 20, 0, 0, TimeSpan.Zero);

        var expected = trip.ToLocalTime().ToString("ddd, dd. MMM · HH:mm", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));

        Assert.Equal(expected, RelativeDateFormatter.Format(trip, now));
    }

    [Fact]
    public void UtcTimestampAroundMidnight_UsesTheLocalCalendarDay()
    {
        // 23:30 UTC on the 9th is already the 10th in CEST (UTC+2) — the label must say "Heute".
        var trip = new DateTimeOffset(2024, 6, 9, 23, 30, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2024, 6, 10, 12, 0, 0, TimeSpan.Zero);

        var expected = trip.ToLocalTime().Date == now.ToLocalTime().Date ? "Heute" : "Gestern";

        Assert.StartsWith(expected, RelativeDateFormatter.Format(trip, now));
    }
}
