using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Models.Trip;
using UniTracks.Services.Trips;
using UniTracks.ViewModels.Pages.Tabs;

namespace UniTracks.ViewModels.Controls.Popups;

/// <summary>
/// The trip filter sheet. Chip values are toggled live; the counter at the bottom answers "how many
/// trips would remain?" before the sheet is closed, so the user never has to guess.
/// <para>
/// The type chips are built from the trips that actually exist (facets), not from the full type
/// catalogue — a sheet that lists 80 types the user has never recorded would be unusable.
/// </para>
/// </summary>
public partial class TripFilterPopupViewModel : ObservableObject, IPopupResultProvider<TripFilters?>
{
    /// <summary>Raised with the new filter, or <c>null</c> when the user cancelled.</summary>
    public event EventHandler<TripFilters?>? Completed;

    private IReadOnlyList<Trip> trips = Array.Empty<Trip>();

    private TripFilters filters = TripFilters.Empty;

    public ObservableCollection<TripFilterChip> TypeChips { get; } = new();

    public ObservableCollection<TripFilterChip> DateChips { get; } = new();

    public ObservableCollection<TripFilterChip> DurationChips { get; } = new();

    public ObservableCollection<TripFilterChip> DistanceChips { get; } = new();

    public IReadOnlyList<DateRangePreset> DatePresets { get; } = new[]
    {
        DateRangePreset.All,
        DateRangePreset.Last7Days,
        DateRangePreset.Last30Days,
        DateRangePreset.ThisYear,
    };

    public IReadOnlyList<DurationBucket> DurationBuckets { get; } = new[]
    {
        DurationBucket.Under30Minutes,
        DurationBucket.Between30And60Minutes,
        DurationBucket.Over60Minutes,
    };

    public IReadOnlyList<DistanceBucket> DistanceBuckets { get; } = new[]
    {
        DistanceBucket.Under5Kilometers,
        DistanceBucket.From5To10Kilometers,
        DistanceBucket.From10To21Kilometers,
        DistanceBucket.HalfMarathonAndMore,
    };

    /// <summary>Whether the type group has anything to offer; a user without types sees no empty row.</summary>
    public bool HasTypeChips => TypeChips.Count > 0;

    /// <summary>How many trips the current selection would leave.</summary>
    [ObservableProperty]
    private int resultCount;

    /// <summary>
    /// The counter doubles as the apply button. Applying a selection without matches is pointless, so
    /// the button is disabled then and the sheet only offers "Zurücksetzen"/"Abbrechen".
    /// </summary>
    public bool CanApply => ResultCount > 0;

    /// <summary>Whether the selection differs from "everything", used for the reset button.</summary>
    public bool HasSelection => filters.HasAny;

    /// <summary>The button label, so the sheet and the tests share one wording.</summary>
    public string ApplyLabel => ResultCount == 1 ? "1 Trip anzeigen" : $"{ResultCount} Trips anzeigen";

    partial void OnResultCountChanged(int value)
    {
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(ApplyLabel));
    }

    /// <summary>Prefills the sheet with the trips to count against and the filter in use.</summary>
    public Task InitializeAsync(IReadOnlyList<Trip> trips, TripFilters current)
    {
        this.trips = trips;
        filters = current;

        RebuildTypeChips();
        RebuildValueChips();

        OnPropertyChanged(nameof(HasTypeChips));

        SyncChips();

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Apply() => Completed?.Invoke(this, filters);

    [RelayCommand]
    private void Cancel() => Completed?.Invoke(this, null);

    [RelayCommand]
    private void Reset()
    {
        filters = TripFilters.Empty;
        SyncChips();
    }

    private void RebuildTypeChips()
    {
        TypeChips.Clear();

        var facets = trips
            .Where(trip => trip.TripTypeId is not null)
            .GroupBy(trip => trip.TripTypeId!.Value)
            .Select(group => new
            {
                Id = group.Key,
                Count = group.Count(),
                Name = group.Select(trip => trip.TripType?.Name).FirstOrDefault(name => !string.IsNullOrEmpty(name)),
            })
            .OrderByDescending(facet => facet.Count)
            .ThenBy(facet => facet.Name, StringComparer.CurrentCultureIgnoreCase);

        foreach (var facet in facets)
        {
            var type = trips.FirstOrDefault(trip => trip.TripTypeId == facet.Id)?.TripType;
            var chip = new TripFilterChip(
                facet.Id,
                facet.Name ?? "Ohne Namen",
                TripTypeVisuals.For(type).Icon,
                facet.Count,
                OnTypeChipToggled);

            TypeChips.Add(chip);
        }
    }

    private void RebuildValueChips()
    {
        DateChips.Clear();
        foreach (var preset in DatePresets)
        {
            DateChips.Add(new TripFilterChip(
                null,
                TripFilterLabels.For(preset),
                string.Empty,
                null,
                _ => SetDateRange(preset)));
        }

        DurationChips.Clear();
        foreach (var bucket in DurationBuckets)
        {
            DurationChips.Add(new TripFilterChip(
                null,
                TripFilterLabels.For(bucket),
                string.Empty,
                null,
                _ => ToggleDuration(bucket)));
        }

        DistanceChips.Clear();
        foreach (var bucket in DistanceBuckets)
        {
            DistanceChips.Add(new TripFilterChip(
                null,
                TripFilterLabels.For(bucket),
                string.Empty,
                null,
                _ => ToggleDistance(bucket)));
        }
    }

    private void OnTypeChipToggled(TripFilterChip chip)
    {
        if (chip.TypeId is { } typeId)
        {
            filters = filters.ToggleType(typeId);
        }

        SyncChips();
    }

    private void SetDateRange(DateRangePreset preset)
    {
        filters = filters.SetDateRange(preset);
        SyncChips();
    }

    private void ToggleDuration(DurationBucket bucket)
    {
        filters = filters.ToggleDuration(bucket);
        SyncChips();
    }

    private void ToggleDistance(DistanceBucket bucket)
    {
        filters = filters.ToggleDistance(bucket);
        SyncChips();
    }

    /// <summary>
    /// Pushes the filter state back into the chips and recomputes the counter. A single entry point so
    /// no toggle can forget the counter; the date group is exclusive, the others allow several values.
    /// </summary>
    private void SyncChips()
    {
        foreach (var chip in TypeChips)
        {
            chip.IsSelected = chip.TypeId is { } typeId && filters.TypeIds.Contains(typeId);
        }

        for (var i = 0; i < DateChips.Count; i++)
        {
            DateChips[i].IsSelected = DatePresets[i] == filters.DateRange;
        }

        for (var i = 0; i < DurationChips.Count; i++)
        {
            DurationChips[i].IsSelected = filters.Durations.Contains(DurationBuckets[i]);
        }

        for (var i = 0; i < DistanceChips.Count; i++)
        {
            DistanceChips[i].IsSelected = filters.Distances.Contains(DistanceBuckets[i]);
        }

        ResultCount = trips.Count(filters.Matches);

        OnPropertyChanged(nameof(HasSelection));
    }
}
