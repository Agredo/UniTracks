using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Models.Trip;
using UniTracks.Services.Comparison;

namespace UniTracks.ViewModels.Controls.Popups;

/// <summary>
/// Outcome of the trip edit popup: the trimmed name and note (null when left empty) and the chosen
/// type. Null as a whole when the user cancelled, so no edit is applied.
/// </summary>
public sealed record TripEditResult(string? Name, string? Description, TripType? TripType);

/// <summary>
/// Edits a recorded trip's metadata: a free-form name (empty keeps the automatic time-of-day name),
/// a note, and the trip type. The type picker is the same search popup the record page uses.
/// </summary>
public partial class TripEditPopupViewModel : ObservableObject, IPopupResultProvider<TripEditResult?>
{
    public event EventHandler<TripEditResult?>? Completed;

    private readonly IPopupNavigationService popupNavigation;
    private readonly ITripTypeCatalog tripTypes;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTypeName))]
    private TripType? selectedType;

    /// <summary>Shown on the type row; falls back to a hint when the trip has no (resolvable) type.</summary>
    public string SelectedTypeName => SelectedType?.Name ?? "Kein Typ";

    public TripEditPopupViewModel(IPopupNavigationService popupNavigation, ITripTypeCatalog tripTypes)
    {
        this.popupNavigation = popupNavigation;
        this.tripTypes = tripTypes;
    }

    /// <summary>
    /// Prefills the fields from the trip. The type is resolved through the catalogue because the
    /// navigation property is not loaded on every store (LiteDB only keeps the id).
    /// </summary>
    public async Task InitializeAsync(Trip trip)
    {
        Name = trip.Name ?? string.Empty;
        Description = trip.Description ?? string.Empty;
        SelectedType = trip.TripType ?? await tripTypes.GetAsync(trip.TripTypeId);
    }

    [RelayCommand]
    private async Task ChangeType()
    {
        var selected = await popupNavigation.ShowPopupAsync<TripTypeSearchPopupViewModel, TripType?>();
        if (selected is not null)
        {
            SelectedType = selected;
        }
    }

    [RelayCommand]
    private void Save() => Completed?.Invoke(this, new TripEditResult(
        string.IsNullOrWhiteSpace(Name) ? null : Name.Trim(),
        string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        SelectedType));

    [RelayCommand]
    private void Cancel() => Completed?.Invoke(this, null);
}
