using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace UniTracks.ViewModels.Pages.Tabs;

/// <summary>
/// One selectable value in the filter sheet. A chip is a state, not a command: tapping it toggles the
/// value, and the sheet's own view model decides what a toggle means in that group.
/// </summary>
public sealed partial class TripFilterChip : ObservableObject
{
    private readonly Action<TripFilterChip> toggle;

    /// <param name="typeId">
    /// The trip type behind the chip, or <c>null</c> for the values that are not trip types (date
    /// presets, duration and distance buckets).
    /// </param>
    /// <param name="icon">Icon file name, empty for the groups whose values are plain text.</param>
    /// <param name="count">How many trips currently carry this value, or <c>null</c> to show none.</param>
    public TripFilterChip(Guid? typeId, string label, string icon, int? count, Action<TripFilterChip> toggle)
    {
        this.toggle = toggle;
        TypeId = typeId;
        Label = label;
        Icon = icon;
        Count = count;
    }

    public Guid? TypeId { get; }

    public string Label { get; }

    public string Icon { get; }

    public int? Count { get; }

    public bool HasIcon => !string.IsNullOrEmpty(Icon);

    public bool HasCount => Count.HasValue;

    public string CountText => Count?.ToString() ?? string.Empty;

    [ObservableProperty]
    private bool isSelected;

    [RelayCommand]
    private void Toggle() => toggle(this);
}
