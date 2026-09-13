using System.Collections.ObjectModel;
using System.Globalization;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;

namespace UniTracks.ViewModels.Pages;

/// <summary>One run of the same route, as a row in the history list.</summary>
public sealed partial class RouteAttemptItem : ObservableObject
{
    public required string Number { get; init; }

    public required string Name { get; init; }

    public required string DateText { get; init; }

    /// <summary>Equivalent pace as mm:ss, the value the rows are ordered by.</summary>
    public required string PaceText { get; init; }

    /// <summary>The same pace in seconds, used to pick the basis of a multiple comparison.</summary>
    public required double PaceSeconds { get; init; }

    public required string DistanceText { get; init; }

    /// <summary>e.g. "+0:42" against the best time of the same trip type; empty for the best itself.</summary>
    public required string DeltaText { get; init; }

    /// <summary>Name of the trip type when it differs from the opened trip's type.</summary>
    public string? ForeignTypeName { get; init; }

    public bool IsBest { get; init; }

    public bool IsCurrent { get; init; }

    public bool IsForeignType => ForeignTypeName is not null;

    /// <summary>The trip this row stands for, handed to the comparison page.</summary>
    public required Trip Trip { get; init; }

    /// <summary>True while the list is picking runs for a multiple comparison; shows the check circle.</summary>
    [ObservableProperty]
    private bool isSelecting;

    /// <summary>This run is part of the current selection.</summary>
    [ObservableProperty]
    private bool isSelected;
}

/// <summary>A trip that is comparable to the opened one, as a row in the candidate list.</summary>
public sealed record RelatedTripItem
{
    public required string Name { get; init; }

    public required string DateText { get; init; }

    public required string DistanceText { get; init; }

    public required string PaceText { get; init; }

    /// <summary>Plain-language reason this trip is listed, e.g. "Gleiche Strecke, umgekehrt".</summary>
    public required string ReasonText { get; init; }

    public string? TypeText { get; init; }

    public required Trip Trip { get; init; }
}

/// <summary>
/// The comparison entry point for one trip: how often the route was run, how the times developed, and
/// which other trips are worth comparing against.
/// </summary>
public partial class TripComparePageViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private readonly ITripSimilarityService similarity;

    public INavigationService Navigation { get; }

    [ObservableProperty]
    private Trip? trip;

    [ObservableProperty]
    private string tripName = "Trip";

    [ObservableProperty]
    private string dateText = string.Empty;

    [ObservableProperty]
    private string headline = string.Empty;

    [ObservableProperty]
    private string trendText = string.Empty;

    [ObservableProperty]
    private bool hasTrend;

    /// <summary>Explains which sport type the results are limited to.</summary>
    [ObservableProperty]
    private string? typeNotice;

    [ObservableProperty]
    private bool hasTypeNotice;

    /// <summary>True while the one-time indexing still runs; the results may still grow.</summary>
    [ObservableProperty]
    private bool isIndexing;

    /// <summary>
    /// The "alle Lauftypen einbeziehen" filter. Relaxes the fine trip type so e.g. a Trail Run is
    /// found for a Run — the sport family and the personal best stay strict regardless.
    /// </summary>
    [ObservableProperty]
    private bool includeAllTripTypes;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasSameRoute;

    [ObservableProperty]
    private bool hasComparableEffort;

    /// <summary>True when trips were found that share part of this route without being the same route.</summary>
    [ObservableProperty]
    private bool hasSimilarRoutes;

    [ObservableProperty]
    private bool hasNothing;

    [ObservableProperty]
    private string emptyText = string.Empty;

    /// <summary>The history list is picking runs for a multiple comparison.</summary>
    [ObservableProperty]
    private bool isSelecting;

    /// <summary>Number of runs picked so far.</summary>
    [ObservableProperty]
    private int selectedCount;

    /// <summary>At least two runs are picked, so a comparison can start.</summary>
    [ObservableProperty]
    private bool canCompareSelected;

    /// <summary>A run was tapped while the cap is reached.</summary>
    [ObservableProperty]
    private bool isSelectionFull;

    public ObservableCollection<RouteAttemptItem> Attempts { get; } = new();

    /// <summary>Trips that partly share this route; shown on their own so they cannot be mistaken for repeats.</summary>
    public ObservableCollection<RelatedTripItem> SimilarTrips { get; } = new();

    public ObservableCollection<RelatedTripItem> ComparableTrips { get; } = new();

    public TripComparePageViewModel(INavigationService navigation, ITripSimilarityService similarity)
    {
        Navigation = navigation;
        this.similarity = similarity;

        Navigation.Parameters.TryGetValue("parameter", out var parameter);
        Trip = parameter as Trip;

        if (Trip is not null)
        {
            TripName = TripDisplay.Name(Trip);
            DateText = Trip.StartTime.LocalDateTime.ToString("dddd, dd. MMMM yyyy · HH:mm", GermanCulture);
        }

        _ = LoadAsync();
    }

    /// <summary>
    /// Re-runs the search whenever the filter flips. The command is fire-and-forget by design: the
    /// generated setter cannot await, and the page shows the loading state from inside LoadAsync.
    /// </summary>
    partial void OnIncludeAllTripTypesChanged(bool value) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        if (Trip is null)
        {
            HasNothing = true;
            EmptyText = "Kein Trip ausgewählt.";
            return;
        }

        IsLoading = true;

        try
        {
            var report = await similarity.BuildReportAsync(Trip, IncludeAllTripTypes);

            Apply(report);
        }
        catch (Exception)
        {
            HasNothing = true;
            HasSameRoute = false;
            HasSimilarRoutes = false;
            HasComparableEffort = false;
            EmptyText = "Der Vergleich konnte nicht geladen werden.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Apply(TripComparisonReport report)
    {
        TypeNotice = report.TypeNotice;
        HasTypeNotice = !string.IsNullOrWhiteSpace(report.TypeNotice);
        IsIndexing = report.IsIndexing;

        Headline = report.History.Headline;
        TrendText = BuildTrendText(report);
        HasTrend = !string.IsNullOrEmpty(TrendText);

        Attempts.Clear();
        foreach (var attempt in report.History.Attempts)
        {
            var item = ToItem(attempt);
            item.IsSelecting = IsSelecting;
            Attempts.Add(item);
        }

        SimilarTrips.Clear();
        foreach (var similar in report.SimilarRoutes)
        {
            SimilarTrips.Add(ToItem(similar));
        }

        ComparableTrips.Clear();
        foreach (var related in report.ComparableEffort)
        {
            ComparableTrips.Add(ToItem(related));
        }

        HasSameRoute = report.History.HasHistory;
        HasSimilarRoutes = SimilarTrips.Count > 0;
        HasComparableEffort = report.ComparableEffort.Count > 0;
        HasNothing = !HasSameRoute && !HasSimilarRoutes && !HasComparableEffort;
        EmptyText = BuildEmptyText(report);
    }

    /// <summary>
    /// The trend line pairs the slope with what it means, plus the plain first-to-latest difference —
    /// a slope alone means nothing to a runner.
    /// </summary>
    private static string BuildTrendText(TripComparisonReport report)
    {
        if (string.IsNullOrEmpty(report.History.TrendLabel))
        {
            return string.Empty;
        }

        double slope = report.History.TrendSecondsPerKilometerPerMonth;
        string perMonth = $"{(slope >= 0 ? "+" : "−")}{FormatSeconds(Math.Abs(slope))} / Monat";

        return $"{report.History.TrendLabel} · {perMonth}";
    }

    private static string BuildEmptyText(TripComparisonReport report)
    {
        if (report.Fingerprint is null)
        {
            return "Für diesen Trip gibt es keine Streckendaten, deshalb lässt er sich nicht vergleichen.";
        }

        return report.IsIndexing
            ? "Die Trips werden gerade ausgewertet. Schau gleich noch einmal vorbei."
            : "Für diesen Trip gibt es noch keinen Vergleich.";
    }

    private static RouteAttemptItem ToItem(RouteAttempt attempt)
    {
        bool foreign = !attempt.IsSameTripType;
        string? foreignName = foreign
            ? attempt.TripTypeName ?? attempt.Fingerprint.TripIdentifier
            : null;

        return new RouteAttemptItem
        {
            Number = attempt.Number.ToString(GermanCulture),
            Name = TripDisplay.Name(attempt.Trip),
            DateText = attempt.Trip.StartTime.ToString("dd.MM.yyyy", GermanCulture),
            PaceText = FormatPace(attempt.EquivalentPaceSecondsPerKilometer),
            PaceSeconds = attempt.EquivalentPaceSecondsPerKilometer,
            DistanceText = FormatDistance(attempt.Fingerprint.DistanceMeters),
            DeltaText = attempt.IsBest || attempt.DeltaToBestSeconds <= 0.5
                ? string.Empty
                : $"+{FormatSeconds(attempt.DeltaToBestSeconds)}",
            ForeignTypeName = foreignName,
            IsBest = attempt.IsBest,
            IsCurrent = attempt.IsCurrent,
            Trip = attempt.Trip,
        };
    }

    private static RelatedTripItem ToItem(RelatedTrip related)
    {
        var fingerprint = related.Fingerprint;

        return new RelatedTripItem
        {
            Name = TripDisplay.Name(related.Trip),
            DateText = related.Trip.StartTime.ToString("dd.MM.yyyy", GermanCulture),
            DistanceText = FormatDistance(fingerprint.DistanceMeters),
            PaceText = FormatPace(EffortModel.EquivalentPaceSecondsPerKilometer(
                fingerprint.DistanceMeters,
                fingerprint.MovingSeconds,
                fingerprint.ElapsedSeconds,
                fingerprint.ElevationGainMeters)),
            ReasonText = related.Match.Label,
            TypeText = fingerprint.TripIdentifier,
            Trip = related.Trip,
        };
    }

    /// <summary>Opens the comparison of two or more runs; the first entry is the basis.</summary>
    private Task OpenComparison(Trip basis, IReadOnlyList<Trip> others) =>
        Navigation.ShellNavigationTo("TripComparisonPage", new Dictionary<string, object>
        {
            { "parameter", basis },
            { "others", others },
        });

    /// <summary>Starts or ends picking runs out of the history list.</summary>
    [RelayCommand]
    private void ToggleSelecting()
    {
        IsSelecting = !IsSelecting;
        IsSelectionFull = false;

        if (IsSelecting)
        {
            // The opened run is the natural starting point.
            var current = Attempts.FirstOrDefault(item => item.IsCurrent);
            if (current is not null)
            {
                current.IsSelected = true;
            }
        }
        else
        {
            foreach (var item in Attempts)
            {
                item.IsSelected = false;
            }
        }

        foreach (var item in Attempts)
        {
            item.IsSelecting = IsSelecting;
        }

        UpdateSelection();
    }

    /// <summary>Picks or drops one run while the history list is in selection mode.</summary>
    [RelayCommand]
    private void Choose(RouteAttemptItem? item)
    {
        if (item is null || !IsSelecting)
        {
            return;
        }

        if (!item.IsSelected && SelectedCount >= TripComparisonPalette.MaxMembers)
        {
            IsSelectionFull = true;
            return;
        }

        item.IsSelected = !item.IsSelected;
        IsSelectionFull = false;
        UpdateSelection();
    }

    /// <summary>Compares the picked runs; the fastest picked run becomes the basis.</summary>
    [RelayCommand]
    private async Task CompareSelected()
    {
        if (Trip is null)
        {
            return;
        }

        var picked = Attempts
            .Where(item => item.IsSelected)
            .OrderBy(item => item.PaceSeconds)
            .ToList();

        if (picked.Count < 2)
        {
            return;
        }

        await OpenComparison(picked[0].Trip, picked.Skip(1).Select(item => item.Trip).ToList());
    }

    /// <summary>Compares the opened trip against all similar routes at once.</summary>
    [RelayCommand]
    private async Task CompareSimilar()
    {
        if (Trip is null || SimilarTrips.Count == 0)
        {
            return;
        }

        var others = SimilarTrips
            .Take(TripComparisonPalette.MaxMembers - 1)
            .Select(item => item.Trip)
            .ToList();

        await OpenComparison(Trip, others);
    }

    private void UpdateSelection()
    {
        SelectedCount = Attempts.Count(item => item.IsSelected);
        CanCompareSelected = SelectedCount >= 2;
    }

    /// <summary>Opens the side-by-side comparison of the current trip and the tapped one.</summary>
    [RelayCommand]
    private async Task Compare(RelatedTripItem? item)
    {
        if (item is null || Trip is null)
        {
            return;
        }

        await Navigation.ShellNavigationTo("TripComparisonPage", new Dictionary<string, object>
        {
            { "parameter", Trip },
            { "other", item.Trip },
        });
    }

    /// <summary>Taps a run: while picking it is toggled, otherwise the 1:1 comparison opens.</summary>
    [RelayCommand]
    private async Task OpenAttempt(RouteAttemptItem? item)
    {
        if (IsSelecting)
        {
            Choose(item);
            return;
        }

        await CompareAttempt(item);
    }

    /// <summary>Opens the side-by-side comparison of the current trip and one of its own previous runs.</summary>
    private async Task CompareAttempt(RouteAttemptItem? item)
    {
        if (item is null || Trip is null || item.IsCurrent)
        {
            return;
        }

        await Navigation.ShellNavigationTo("TripComparisonPage", new Dictionary<string, object>
        {
            { "parameter", Trip },
            { "other", item.Trip },
        });
    }

    private static string FormatPace(double secondsPerKilometer)
    {
        if (secondsPerKilometer <= 0)
        {
            return "-";
        }

        return $"{FormatSeconds(secondsPerKilometer)} /km";
    }

    private static string FormatSeconds(double totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Round(totalSeconds));
        return span.TotalHours >= 1
            ? span.ToString(@"h\:mm\:ss", GermanCulture)
            : span.ToString(@"m\:ss", GermanCulture);
    }

    private static string FormatDistance(double meters) =>
        meters >= 1000
            ? (meters / 1000.0).ToString("0.00", GermanCulture) + " km"
            : Math.Round(meters).ToString("0", GermanCulture) + " m";
}
