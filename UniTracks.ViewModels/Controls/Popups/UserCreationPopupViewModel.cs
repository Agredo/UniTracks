using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.User;

namespace UniTracks.ViewModels.Controls.Popups;

public partial class UserCreationPopupViewModel : ObservableObject, IPopupResultProvider<bool>
{
    public event EventHandler<bool>? Completed;

    public IGenericRepository<SqliteDBContext> SqliteRepository { get; }

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string height = string.Empty;

    [ObservableProperty]
    private string weight = string.Empty;

    public UserCreationPopupViewModel(IGenericRepository<SqliteDBContext> sqliteRepository)
    {
        SqliteRepository = sqliteRepository;
    }

    [RelayCommand]
    private async Task CreateUser()
    {
        var user = new User() { Name = Name, Email = Email, Password = Password };
        await SqliteRepository.Add(user);

        Completed?.Invoke(this, true);
    }

    [RelayCommand]
    private async Task Cancel()
    {
        var user = new User() { Name = "User" };
        await SqliteRepository.Add(user);

        Completed?.Invoke(this, true);
    }
}
