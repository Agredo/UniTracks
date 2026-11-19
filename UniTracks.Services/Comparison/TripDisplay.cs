using UniTracks.Models.Trip;

namespace UniTracks.Services.Comparison;

public enum TripRateKind
{
    PacePerKilometer,
    PacePer100Meters,
    SpeedKilometersPerHour,
}

/// <summary>
/// The name shown for a trip. Trips recorded without a name get a time-of-day label, which is the
/// convention the trip list and the trip detail page already use.
/// </summary>
public static class TripDisplay
{
    public static string Name(Trip trip)
    {
        if (!string.IsNullOrWhiteSpace(trip.Name))
        {
            return trip.Name;
        }

        return TimeOfDayName(trip.StartTime);
    }

    public static TripRateKind RateKind(TripType? type)
    {
        if (type is null)
        {
            return TripRateKind.PacePerKilometer;
        }

        var category = type?.Category ?? string.Empty;
        var identifier = type?.Identifier ?? string.Empty;

        if (category.Equals("running", StringComparison.OrdinalIgnoreCase))
        {
            return TripRateKind.PacePerKilometer;
        }

        if (identifier.Contains("swimming", StringComparison.OrdinalIgnoreCase))
        {
            return TripRateKind.PacePer100Meters;
        }

        return TripRateKind.SpeedKilometersPerHour;
    }

    public static string RateLabel(TripType? type) => RateKind(type) switch
    {
        TripRateKind.PacePerKilometer => "PACE /KM",
        TripRateKind.PacePer100Meters => "PACE /100 M",
        _ => "Ø KM/H",
    };

    public static string RateChartTitle(TripType? type) =>
        RateKind(type) == TripRateKind.SpeedKilometersPerHour
            ? "GESCHWINDIGKEITSVERLAUF"
            : "TEMPOVERLAUF";

    public static string RateAxisUnit(TripType? type) => RateKind(type) switch
    {
        TripRateKind.PacePerKilometer => "min/km",
        TripRateKind.PacePer100Meters => "min/100 m",
        _ => "km/h",
    };

    public static bool IsPace(TripType? type) =>
        RateKind(type) != TripRateKind.SpeedKilometersPerHour;

    public static double RawRateSeconds(TripType? type, double distanceMeters, double movingSeconds)
    {
        if (distanceMeters <= 0 || movingSeconds <= 0)
        {
            return 0;
        }

        double paceSecondsPerKilometer = movingSeconds / (distanceMeters / 1000.0);
        return RateKind(type) == TripRateKind.PacePer100Meters
            ? paceSecondsPerKilometer / 10.0
            : paceSecondsPerKilometer;
    }

    /// <summary>Converts the shared seconds-per-kilometre model into the activity's display unit.</summary>
    public static double RateValue(TripType? type, double secondsPerKilometer)
    {
        if (secondsPerKilometer <= 0)
        {
            return 0;
        }

        return RateKind(type) switch
        {
            TripRateKind.PacePer100Meters => secondsPerKilometer / 10.0,
            TripRateKind.SpeedKilometersPerHour => 3600.0 / secondsPerKilometer,
            _ => secondsPerKilometer,
        };
    }

    public static string FormatRate(TripType? type, double secondsPerKilometer)
    {
        if (secondsPerKilometer <= 0)
        {
            return "-";
        }

        return RateKind(type) == TripRateKind.SpeedKilometersPerHour
            ? RateValue(type, secondsPerKilometer).ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))
            : FormatPaceSeconds(RateValue(type, secondsPerKilometer));
    }

    public static string FormatRateWithUnit(TripType? type, double secondsPerKilometer)
    {
        if (secondsPerKilometer <= 0)
        {
            return "-";
        }

        return RateKind(type) switch
        {
            TripRateKind.PacePerKilometer => FormatRate(type, secondsPerKilometer) + " /km",
            TripRateKind.PacePer100Meters => FormatRate(type, secondsPerKilometer) + " /100 m",
            _ => FormatRate(type, secondsPerKilometer) + " km/h",
        };
    }

    /// <summary>Signed B-minus-A delta in the unit the athlete sees.</summary>
    public static string FormatRateDelta(
        TripType? type,
        double secondsPerKilometerA,
        double secondsPerKilometerB,
        double flatBelow = 0.5)
    {
        if (secondsPerKilometerA <= 0 || secondsPerKilometerB <= 0)
        {
            return "-";
        }

        double valueA = RateValue(type, secondsPerKilometerA);
        double valueB = RateValue(type, secondsPerKilometerB);
        double delta = valueB - valueA;

        if (Math.Abs(delta) < flatBelow)
        {
            return "gleich";
        }

        return RateKind(type) == TripRateKind.SpeedKilometersPerHour
            ? (delta > 0 ? "+" : "−") + Math.Abs(delta).ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"))
            : (delta > 0 ? "+" : "−") + FormatPaceSeconds(Math.Abs(delta));
    }

    public static string FormatRateMagnitude(TripType? type, double secondsPerKilometerA, double secondsPerKilometerB)
    {
        if (secondsPerKilometerA <= 0 || secondsPerKilometerB <= 0)
        {
            return "-";
        }

        double delta = Math.Abs(RateValue(type, secondsPerKilometerB) - RateValue(type, secondsPerKilometerA));
        if (RateKind(type) == TripRateKind.SpeedKilometersPerHour)
        {
            return delta.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " km/h";
        }

        string unit = RateKind(type) == TripRateKind.PacePer100Meters ? "/100 m" : "/km";
        return delta < 60
            ? Math.Round(delta).ToString("0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " s" + unit
            : FormatPaceSeconds(delta) + " " + unit;
    }

    public static string FormatPaceSeconds(double seconds)
    {
        var value = TimeSpan.FromSeconds(Math.Max(0, Math.Round(seconds)));
        return $"{(int)value.TotalMinutes}:{value.Seconds:00}";
    }

    public static double SecondsPerKilometer(double distanceMeters, double movingSeconds) =>
        distanceMeters <= 0 || movingSeconds <= 0
            ? 0
            : movingSeconds / (distanceMeters / 1000.0);

    public static string TimeOfDayName(DateTimeOffset startTime) => startTime.LocalDateTime.Hour switch
    {
        >= 5 and < 11 => "Morgen Trip",
        >= 11 and < 14 => "Mittags Trip",
        >= 14 and < 18 => "Nachmittags Trip",
        _ => "Abend Trip",
    };
}
