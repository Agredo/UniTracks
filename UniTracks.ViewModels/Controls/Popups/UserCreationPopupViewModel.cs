using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniTracks.Data.Repository;
using UniTracks.Data.SQLite;
using UniTracks.Models.User;

namespace UniTracks.ViewModels.Controls.Popups;

public delegate Task CloseHandler<T>(T result);

public partial class UserCreationPopupViewModel : ObservableObject
{
    public event CloseHandler<bool> OnClose;

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
    private void CreateUser()
    {
        var user = new User() { Name = name, Email = email, Password = password };
        SqliteRepository.Add(user);

        OnClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        //CreateDefaultUser
        var user = new User() { Name = "User" };
        SqliteRepository.Add(user);

        OnClose?.Invoke(true);
    }
}
