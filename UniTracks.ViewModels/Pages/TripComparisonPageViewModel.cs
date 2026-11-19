using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using UniTracks.Models.Comparison;
using UniTracks.Models.Location;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;

namespace UniTracks.ViewModels.Pages;

/// <summary>One row of the metric table: the value of both trips and their difference.</summary>
public sealed record MetricRow
{
    public required string Label { get; init; }

    public required string ValueA { get; init; }

    public required string ValueB { get; init; }

    /// <summary>Difference of B against A, e.g. "+0:42" or "gleich" inside the flat band.</summary>
    public required string DeltaText { get; init; }

    /// <summary>The difference favours trip B.</summary>
    public bool DeltaIsGoodForB { get; init; }

    /// <summary>Whether a difference beyond the flat band says anything about the run at all.</summary>
    public bool DeltaIsMeaningful { get; init; }
}

/// <summary>One kilometre split with the moving time of both trips.</summary>
public sealed record SplitRow
{
    public required string Kilometer { get; init; }

    public required string ValueA { get; init; }

    public required string ValueB { get; init; }

    public required string DeltaText { get; init; }

    /// <summary>B was faster on this split.</summary>
    public bool BIsFaster { get; init; }

    public bool IsPartial { get; init; }
}

/// <summary>One line of the ranking table: everything about one run in a single row.</summary>
public sealed record LeaderboardRow
{
    public required int Rank { get; init; }

    public required string Name { get; init; }

    public required string ColorHex { get; init; }

    /// <summary>Moving time, e.g. "24:31".</summary>
    public required string TimeText { get; init; }

    /// <summary>Actual activity rate, e.g. "4:58 /km" or "24,5 km/h".</summary>
    public required string PaceText { get; init; }

    /// <summary>"Basis" for the reference run, otherwise the pace difference against it.</summary>
    public required string DeltaText { get; init; }

    public bool DeltaIsFaster { get; init; }

    /// <summary>Date, distance and climb, e.g. "08.09.2025 · 7,20 km · 142 m hoch".</summary>
    public required string MetaText { get; init; }

    public bool IsBaseline { get; init; }

    public bool IsFastest { get; init; }
}

/// <summary>One run's pace on one kilometre of the split matrix.</summary>
public sealed record SplitCellRow
{
    public required string PaceText { get; init; }

    /// <summary>Difference against the basis on this kilometre, empty for the basis itself.</summary>
    public required string DeltaText { get; init; }

    public bool DeltaIsFaster { get; init; }

    /// <summary>This run was the fastest on this kilometre.</summary>
    public bool IsFastest { get; init; }

    public bool IsPartial { get; init; }
}

/// <summary>One kilometre of the split matrix, with a cell per run.</summary>
public sealed record SplitMatrixRow
{
    public required string Kilometer { get; init; }

    public required IReadOnlyList<SplitCellRow> Cells { get; init; }
}

/// <summary>The column header of the split matrix: one run with its colour and rank.</summary>
public sealed record SplitColumn
{
    public required string Name { get; init; }

    public required string ColorHex { get; init; }

    public required string RankText { get; init; }

    public bool IsBaseline { get; init; }
}

/// <summary>
/// The comparison of two or more trips against a basis: route overlay, key figures or ranking,
/// pace profile and kilometre splits.
/// </summary>
public partial class TripComparisonPageViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private readonly ITripSimilarityService similarity;

    /// <summary>The further trips of the comparison; the trip in the navigation parameter is the basis.</summary>
    private readonly List<Trip> others = new();

    public INavigationService Navigation { get; }

    [ObservableProperty]
    private Trip? tripA;

    [ObservableProperty]
    private Trip? tripB;

    [ObservableProperty]
    private string nameA = "Trip A";

    [ObservableProperty]
    private string nameB = "Trip B";

    [ObservableProperty]
    private string dateA = string.Empty;

    [ObservableProperty]
    private string dateB = string.Empty;

    [ObservableProperty]
    private string contextA = string.Empty;

    [ObservableProperty]
    private string contextB = string.Empty;

    /// <summary>Basis and number of trips, e.g. "Basis Morgenrunde · 4 Trips".</summary>
    [ObservableProperty]
    private string subtitle = string.Empty;

    /// <summary>The one-line summary, e.g. "B war 0:42 schneller bei ähnlicher Strecke".</summary>
    [ObservableProperty]
    private string verdict = string.Empty;

    [ObservableProperty]
    private string verdictDetail = string.Empty;

    /// <summary>States what the comparison cannot answer, e.g. different weather or a part of a lap.</summary>
    [ObservableProperty]
    private string? fairnessNote;

    [ObservableProperty]
    private bool hasFairnessNote;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasComparison;

    [ObservableProperty]
    private bool hasSplits;

    /// <summary>Splits are available and the classic A/B table applies.</summary>
    [ObservableProperty]
    private bool showPairSplits;

    /// <summary>Splits are available and the matrix with one column per run applies.</summary>
    [ObservableProperty]
    private bool showMultiSplits;

    /// <summary>More than two trips: the page shows the ranking and the split matrix instead of A/B.</summary>
    [ObservableProperty]
    private bool isMulti;

    /// <summary>The classic A/B page of exactly two trips.</summary>
    [ObservableProperty]
    private bool isPair;

    [ObservableProperty]
    private string errorText = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private IReadOnlyList<Location> trackA = Array.Empty<Location>();

    /// <summary>The further runs: drawn on top of the basis and listed in the legend.</summary>
    [ObservableProperty]
    private IReadOnlyList<ComparisonTrack> otherTracks = Array.Empty<ComparisonTrack>();

    /// <summary>One actual-tempo curve per trip.</summary>
    [ObservableProperty]
    private IReadOnlyList<ComparisonCurve>? curves;

    [ObservableProperty]
    private string rateChartTitle = "TEMPOVERLAUF";

    [ObservableProperty]
    private string rateAxisUnit = "min/km";

    [ObservableProperty]
    private bool invertRateAxis = true;

    [ObservableProperty]
    private bool isPaceRate = true;

    [ObservableProperty]
    private string rateChartCaption = string.Empty;

    [ObservableProperty]
    private string baselineColor = TripComparisonPalette.ColorFor(0);

    /// <summary>Masks for unavailable comparison paces, parallel to <see cref="Curves"/>.</summary>
    [ObservableProperty]
    private IList<IList<bool>>? missingPaceValues;

    [ObservableProperty]
    private IList<string> distanceAxisLabels = new List<string>();

    /// <summary>Column headers of the split matrix, in the order of the runs.</summary>
    [ObservableProperty]
    private IReadOnlyList<SplitColumn> matrixColumns = Array.Empty<SplitColumn>();

    public IReadOnlyList<MetricRow> Metrics { get; private set; } = Array.Empty<MetricRow>();

    public IReadOnlyList<SplitRow> Splits { get; private set; } = Array.Empty<SplitRow>();

    /// <summary>All runs, fastest first.</summary>
    public IReadOnlyList<LeaderboardRow> Leaderboard { get; private set; } = Array.Empty<LeaderboardRow>();

    public IReadOnlyList<SplitMatrixRow> SplitMatrix { get; private set; } = Array.Empty<SplitMatrixRow>();

    public TripComparisonPageViewModel(INavigationService navigation, ITripSimilarityService similarity)
    {
        Navigation = navigation;
        this.similarity = similarity;

        Navigation.Parameters.TryGetValue("parameter", out var parameter);
        Navigation.Parameters.TryGetValue("other", out var other);
        Navigation.Parameters.TryGetValue("others", out var selection);

        TripA = parameter as Trip;

        if (selection is IEnumerable<Trip> trips)
        {
            others.AddRange(trips.Where(trip => trip.ID != TripA?.ID));
        }

        // The single "other" trip keeps working, so links built before the selection existed still
        // open the comparison they always did.
        if (other is Trip single && single.ID != TripA?.ID && others.All(trip => trip.ID != single.ID))
        {
            others.Add(single);
            TripB = single;
        }

        TripB ??= others.Count > 0 ? others[0] : null;

        if (TripA is not null)
        {
            NameA = TripDisplay.Name(TripA);
            DateA = TripA.StartTime.LocalDateTime.ToString("dd.MM.yyyy · HH:mm", GermanCulture);
        }

        if (TripB is not null)
        {
            NameB = TripDisplay.Name(TripB);
            DateB = TripB.StartTime.LocalDateTime.ToString("dd.MM.yyyy · HH:mm", GermanCulture);
        }

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (TripA is null)
        {
            HasError = true;
            ErrorText = "Für den Vergleich fehlt der Ausgangs-Trip.";
            return;
        }

        if (others.Count == 0)
        {
            HasError = true;
            ErrorText = "Für den Vergleich fehlen die beiden Trips.";
            return;
        }

        IsLoading = true;

        try
        {
            var comparison = await similarity.CompareSetAsync(TripA, others.ToList());

            if (comparison is null)
            {
                HasError = true;
                ErrorText = "Einer der Trips hat keine Streckendaten.";
            }
            else
            {
                Apply(comparison);
            }
        }
        catch (Exception)
        {
            HasError = true;
            ErrorText = "Der Vergleich konnte nicht berechnet werden.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(TripComparisonSet comparison)
    {
        Verdict = comparison.Verdict;
        VerdictDetail = comparison.VerdictDetail;
        FairnessNote = comparison.FairnessNote;
        HasFairnessNote = !string.IsNullOrWhiteSpace(comparison.FairnessNote);
        IsMulti = !comparison.IsPair;
        IsPair = comparison.IsPair;

        var baseline = comparison.Baseline;

        Subtitle = IsMulti
            ? $"Basis {baseline.Name} · {comparison.Count} Trips"
            : $"{NameA} gegen {NameB}";

        TrackA = baseline.Side.Track;
        BaselineColor = baseline.ColorHex;
        ContextA = BuildContext(baseline.Side);
        DistanceAxisLabels = comparison.DistanceAxisLabels.ToList();
        RateChartTitle = TripDisplay.RateChartTitle(baseline.Side.Trip.TripType);
        RateAxisUnit = TripDisplay.RateAxisUnit(baseline.Side.Trip.TripType);
        IsPaceRate = TripDisplay.IsPace(baseline.Side.Trip.TripType);
        InvertRateAxis = IsPaceRate;
        RateChartCaption = IsPaceRate
            ? $"Rohes Tempo in {RateAxisUnit}, ohne Glättung. Alle Kurven teilen Kilometer- und Wertachse."
            : "Rohes Tempo in km/h, ohne Glättung. Alle Kurven teilen Kilometer- und Wertachse.";

        // Every trip gets one route colour, so colour means trip identity throughout the comparison.
        OtherTracks = comparison.Members
            .Skip(1)
            .Select(member => new ComparisonTrack
            {
                Name = member.Name,
                ColorHex = member.ColorHex,
                Locations = member.Side.Track,
            })
            .ToList();
        Curves = BuildCurves(comparison);
        MissingPaceValues = Curves
            .Select(curve => (IList<bool>)curve.Values.Select(value => value <= 0).ToList())
            .ToList();

        if (IsMulti)
        {
            Metrics = Array.Empty<MetricRow>();
            Splits = Array.Empty<SplitRow>();
            Leaderboard = BuildLeaderboard(comparison).ToList();
            SplitMatrix = BuildSplitMatrix(comparison).ToList();
            MatrixColumns = BuildColumns(comparison).ToList();
        }
        else
        {
            Metrics = BuildMetrics(baseline.Side, comparison.Members[1].Side).ToList();
            Splits = BuildPairSplits(comparison).ToList();
            Leaderboard = Array.Empty<LeaderboardRow>();
            SplitMatrix = Array.Empty<SplitMatrixRow>();
            MatrixColumns = Array.Empty<SplitColumn>();
            ContextB = BuildContext(comparison.Members[1].Side);
        }

        OnPropertyChanged(nameof(Metrics));
        OnPropertyChanged(nameof(Splits));
        OnPropertyChanged(nameof(Leaderboard));
        OnPropertyChanged(nameof(SplitMatrix));

        HasSplits = comparison.HasSplits;
        ShowPairSplits = comparison.HasSplits && IsPair;
        ShowMultiSplits = comparison.HasSplits && IsMulti;
        HasComparison = true;
    }

    /// <summary>One actual-tempo curve per run; a missing kilometre stays a chart gap.</summary>
    private static IReadOnlyList<ComparisonCurve> BuildCurves(TripComparisonSet comparison)
    {
        var curves = new List<ComparisonCurve>(comparison.Count);
        var displayType = comparison.Baseline.Side.Trip.TripType;

        for (int index = 0; index < comparison.Count; index++)
        {
            var values = comparison.PaceSeries[index]
                .Select(value => value > 0 ? TripDisplay.RateValue(displayType, value) : 0)
                .ToList();
            if (!values.Any(value => value > 0))
            {
                continue;
            }

            curves.Add(new ComparisonCurve
            {
                Name = comparison.Members[index].Name,
                ColorHex = comparison.Members[index].ColorHex,
                Values = values,
            });
        }

        return curves;
    }

    /// <summary>All trips by actual pace, fastest first, with the difference to the basis.</summary>
    private static IEnumerable<LeaderboardRow> BuildLeaderboard(TripComparisonSet comparison)
    {
        var displayType = comparison.Baseline.Side.Trip.TripType;
        double baselinePace = comparison.Baseline.PaceSecondsPerKilometer;

        foreach (var rank in comparison.Ranking)
        {
            var side = rank.Member.Side;
            var delta = rank.DeltaSecondsPerKilometer;

            yield return new LeaderboardRow
            {
                Rank = rank.Rank,
                Name = rank.Member.Name,
                ColorHex = rank.Member.ColorHex,
                TimeText = FormatSeconds(side.MovingSeconds),
                PaceText = TripDisplay.FormatRateWithUnit(
                    displayType,
                    rank.Member.PaceSecondsPerKilometer),
                DeltaText = rank.IsBaseline
                    ? "Basis"
                    : TripDisplay.FormatRateDelta(
                        displayType,
                        baselinePace,
                        rank.Member.PaceSecondsPerKilometer,
                        0.5),
                DeltaIsFaster = !rank.IsBaseline && delta < -0.5,
                MetaText = BuildContext(side),
                IsBaseline = rank.IsBaseline,
                IsFastest = rank.IsFastest,
            };
        }
    }

    /// <summary>One row per shared kilometre, one cell per run.</summary>
    private static IEnumerable<SplitMatrixRow> BuildSplitMatrix(TripComparisonSet comparison)
    {
        var displayType = comparison.Baseline.Side.Trip.TripType;

        foreach (var row in comparison.Splits)
        {
            var cells = new List<SplitCellRow>(comparison.Count);

            for (int index = 0; index < comparison.Count; index++)
            {
                var delta = row.DeltaSecondsPerMember[index];

                cells.Add(new SplitCellRow
                {
                    PaceText = TripDisplay.FormatRate(displayType, row.SecondsPerMember[index]),
                    DeltaText = index == 0
                        ? string.Empty
                        : TripDisplay.FormatRateDelta(
                            displayType,
                            row.SecondsPerMember[0],
                            row.SecondsPerMember[index],
                            0.5),
                    DeltaIsFaster = index != 0 && delta < -0.5,
                    IsFastest = row.FastestMemberIndex == index,
                    IsPartial = row.IsPartial,
                });
            }

            yield return new SplitMatrixRow
            {
                Kilometer = row.IsPartial
                    ? $"{row.Number} (teilw.)"
                    : row.Number.ToString(GermanCulture),
                Cells = cells,
            };
        }
    }

    private static IEnumerable<SplitColumn> BuildColumns(TripComparisonSet comparison)
    {
        var ranks = comparison.Ranking.ToDictionary(rank => rank.Member.Index, rank => rank.Rank);

        foreach (var member in comparison.Members)
        {
            yield return new SplitColumn
            {
                Name = member.Name,
                ColorHex = member.ColorHex,
                RankText = ranks.TryGetValue(member.Index, out var rank)
                    ? rank.ToString(GermanCulture)
                    : "-",
                IsBaseline = member.IsBaseline,
            };
        }
    }

    private static IEnumerable<SplitRow> BuildPairSplits(TripComparisonSet comparison)
    {
        var displayType = comparison.Baseline.Side.Trip.TripType;

        foreach (var split in comparison.Splits)
        {
            yield return new SplitRow
            {
                Kilometer = split.IsPartial
                    ? $"{split.Number} (teilw.)"
                    : split.Number.ToString(GermanCulture),
                ValueA = TripDisplay.FormatRate(displayType, split.SecondsPerMember[0]),
                ValueB = TripDisplay.FormatRate(displayType, split.SecondsPerMember[1]),
                DeltaText = TripDisplay.FormatRateDelta(
                    displayType,
                    split.SecondsPerMember[0],
                    split.SecondsPerMember[1],
                    0.5),
                BIsFaster = split.DeltaSecondsPerMember[1] < -0.5,
                IsPartial = split.IsPartial,
            };
        }
    }

    private static IEnumerable<MetricRow> BuildMetrics(TripComparisonSide a, TripComparisonSide b)
    {
        var displayType = a.Trip.TripType;
        bool hasRate = a.PaceSecondsPerKilometer > 0 && b.PaceSecondsPerKilometer > 0;
        bool meaningfulRate = hasRate
            && Math.Abs(b.PaceSecondsPerKilometer - a.PaceSecondsPerKilometer)
                >= TripComparisonCalculator.FlatBandSecondsPerKilometer;

        yield return new MetricRow
        {
            Label = "Distanz",
            ValueA = FormatDistance(a.DistanceMeters),
            ValueB = FormatDistance(b.DistanceMeters),
            DeltaText = Signed(b.DistanceMeters - a.DistanceMeters, FormatDistance, 1.0),
            DeltaIsMeaningful = false,
        };

        yield return new MetricRow
        {
            Label = "Gesamtdauer",
            ValueA = FormatSeconds(a.ElapsedSeconds),
            ValueB = FormatSeconds(b.ElapsedSeconds),
            DeltaText = Signed(b.ElapsedSeconds - a.ElapsedSeconds, FormatSeconds, 0.5),
            DeltaIsMeaningful = false,
        };

        yield return new MetricRow
        {
            Label = "Bewegung",
            ValueA = FormatSeconds(a.MovingSeconds),
            ValueB = FormatSeconds(b.MovingSeconds),
            DeltaText = Signed(b.MovingSeconds - a.MovingSeconds, FormatSeconds, 0.5),
            DeltaIsMeaningful = false,
        };

        yield return new MetricRow
        {
            Label = "Pause",
            ValueA = FormatSeconds(a.PausedSeconds),
            ValueB = FormatSeconds(b.PausedSeconds),
            DeltaText = Signed(b.PausedSeconds - a.PausedSeconds, FormatSeconds, 0.5),
            DeltaIsMeaningful = false,
        };

        yield return new MetricRow
        {
            Label = TripDisplay.RateLabel(displayType),
            ValueA = TripDisplay.FormatRate(displayType, a.PaceSecondsPerKilometer),
            ValueB = TripDisplay.FormatRate(displayType, b.PaceSecondsPerKilometer),
            DeltaText = TripDisplay.FormatRateDelta(
                displayType,
                a.PaceSecondsPerKilometer,
                b.PaceSecondsPerKilometer),
            DeltaIsMeaningful = meaningfulRate,
            DeltaIsGoodForB = meaningfulRate && b.PaceSecondsPerKilometer < a.PaceSecondsPerKilometer,
        };

        yield return new MetricRow
        {
            Label = TripDisplay.IsPace(displayType)
                ? "PACE, MODELL"
                : "TEMPO, MODELL",
            ValueA = TripDisplay.FormatRate(displayType, a.EquivalentPaceSecondsPerKilometer),
            ValueB = TripDisplay.FormatRate(displayType, b.EquivalentPaceSecondsPerKilometer),
            DeltaText = TripDisplay.FormatRateDelta(
                displayType,
                a.EquivalentPaceSecondsPerKilometer,
                b.EquivalentPaceSecondsPerKilometer),
            DeltaIsMeaningful = false,
        };

        yield return new MetricRow
        {
            Label = "Höhenmeter",
            ValueA = Math.Round(a.ElevationGainMeters).ToString("0", GermanCulture) + " m",
            ValueB = Math.Round(b.ElevationGainMeters).ToString("0", GermanCulture) + " m",
            DeltaText = Signed(
                b.ElevationGainMeters - a.ElevationGainMeters,
                value => Math.Round(value).ToString("0", GermanCulture),
                1.0),
            DeltaIsMeaningful = false,
        };

        var averageA = AverageHeartRate(a);
        var averageB = AverageHeartRate(b);
        if (averageA > 0 && averageB > 0)
        {
            yield return new MetricRow
            {
                Label = "Ø Puls",
                ValueA = Math.Round(averageA).ToString("0", GermanCulture) + " bpm",
                ValueB = Math.Round(averageB).ToString("0", GermanCulture) + " bpm",
                DeltaText = Signed(averageB - averageA, value => Math.Round(value).ToString("0", GermanCulture), 0.5),
                DeltaIsMeaningful = false,
            };
        }
    }

    private static string BuildContext(TripComparisonSide side)
    {
        var parts = new List<string>
        {
            side.Trip.StartTime.ToString("dd.MM.yyyy", GermanCulture),
            FormatDistance(side.DistanceMeters),
            Math.Round(side.ElevationGainMeters).ToString("0", GermanCulture) + " m hoch",
        };

        if (!string.IsNullOrWhiteSpace(side.Trip.TripType?.Name))
        {
            parts.Insert(1, side.Trip.TripType.Name);
        }

        double averageHeartRate = AverageHeartRate(side);
        if (averageHeartRate > 0)
        {
            parts.Add(Math.Round(averageHeartRate).ToString("0", GermanCulture) + " bpm");
        }

        if (side.Trip.Weather?.Count > 0)
        {
            double temperature = side.Trip.Weather.Average(weather => weather.Temperature);
            parts.Add(Math.Round(temperature).ToString("0", GermanCulture) + "°");
        }

        return string.Join(" · ", parts);
    }

    private static double AverageHeartRate(TripComparisonSide side) =>
        side.Trip.HeartRates?.Count > 0
            ? side.Trip.HeartRates.Average(rate => rate.Rate)
            : 0;

    /// <summary>Signed difference against A, or "gleich" when it stays inside the flat band.</summary>
    private static string Signed(double delta, Func<double, string> format, double flatBelow)
    {
        if (Math.Abs(delta) < flatBelow)
        {
            return "gleich";
        }

        return (delta > 0 ? "+" : "−") + format(Math.Abs(delta));
    }

    private static string FormatSeconds(double totalSeconds)
    {
        if (totalSeconds <= 0)
        {
            return "-";
        }

        var time = TimeSpan.FromSeconds(Math.Round(totalSeconds));

        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss", GermanCulture)
            : time.ToString(@"m\:ss", GermanCulture);
    }

    private static string FormatDistance(double meters)
        => meters >= 1000
            ? (meters / 1000).ToString("0.00", GermanCulture) + " km"
            : Math.Round(meters).ToString("0", GermanCulture) + " m";
}
