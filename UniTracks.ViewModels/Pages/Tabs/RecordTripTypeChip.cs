using CommunityToolkit.Mvvm.ComponentModel;
using UniTracks.Models.Trip;
using UniTracks.Services.Trips;

namespace UniTracks.ViewModels.Pages.Tabs;

/// <summary>
/// One type in the record page's quick pick. The chip carries its own visual (the same icon and accent
/// the trip cards use) and its own selected state, so the row does not need a cross-binding back into
/// the page's binding context to highlight the active type.
/// </summary>
public sealed partial class RecordTripTypeChip : ObservableObject
{
    public RecordTripTypeChip(TripType type)
    {
        Type = type;
        Icon = TripTypeVisuals.For(type).Icon;
    }

    public TripType Type { get; }

    public Guid ID => Type.ID;

    public string Label => Type.Name;

    /// <summary>Icon file name of the type, resolved through the same catalog the trip cards use.</summary>
    public string Icon { get; }

    [ObservableProperty]
    private bool isSelected;
}
