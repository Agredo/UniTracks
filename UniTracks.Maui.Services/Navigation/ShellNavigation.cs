namespace UniTracks.Maui.Services.Navigation;

public class ShellNavigation : UniTracks.Services.Navigation.INavigation
{
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    /// <exception cref="ArgumentException">Thrown when route already exists</exception>
    public void RegisterRoute(Type page) =>
        Routing.RegisterRoute(page.Name, page);
    public async Task NavigateTo(string route) => await Shell.Current.GoToAsync(route);
    public async Task NavigateTo(string route, object parameter)
    {
        Parameters = new Dictionary<string, object> { { "parameter", parameter } };
        await Shell.Current.GoToAsync(route);
    }
    public async Task NavigateTo(string Route, Dictionary<string, object> parameters)
    {
        Parameters = parameters;
        await Shell.Current.GoToAsync(Route, parameters);
    }
    public async Task NavigateBack() => await Shell.Current.Navigation.PopAsync();
}
