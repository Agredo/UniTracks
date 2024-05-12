using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Services.Navigation;

public interface INavigation
{
    public Dictionary<string, object> Parameters { get; set; }
    void RegisterRoute(Type page);
    Task NavigateTo(string route);
    Task NavigateTo(string route, object parameter);
    Task NavigateTo(string Route, Dictionary<string, object> parameters);
    Task NavigateBack();
}
