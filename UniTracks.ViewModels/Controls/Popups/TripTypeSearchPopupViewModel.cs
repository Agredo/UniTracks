using System.Collections.ObjectModel;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Models.Trip;

namespace UniTracks.ViewModels.Controls.Popups;

public partial class TripTypeSearchPopupViewModel : ObservableObject, IPopupResultProvider<TripType?>
{
    public event EventHandler<TripType?>? Completed;

    private List<TripType> allTypes = new();

    public IRepository Repository { get; }

    public ObservableCollection<TripType> Types { get; } = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    public TripTypeSearchPopupViewModel(IRepository repository)
    {
        Repository = repository;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        allTypes = (await Repository.GetAllAsync<TripType>())
            .OrderBy(t => t.Name)
            .ToList();
        ApplyFilter(SearchText);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter(value);

    private void ApplyFilter(string value)
    {
        Types.Clear();
        IEnumerable<TripType> query = allTypes;
        if (!string.IsNullOrWhiteSpace(value))
        {
            query = query.Where(t =>
                t.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                t.Identifier.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var type in query)
        {
            Types.Add(type);
        }
    }

    public void SelectType(TripType? type)
    {
        if (type is null)
        {
            return;
        }

        Completed?.Invoke(this, type);
    }

    [RelayCommand]
    private void Cancel()
    {
        Completed?.Invoke(this, null);
    }
}
