using MauiDispatcher = Microsoft.Maui.Dispatching.Dispatcher;
using UniTracks.Maui.Services.Dispatching;

namespace UniTracks.Maui.Services.Dispatching;

public class Dispatcher : UniTracks.Services.Dispatching.IDispatcher
{
    public IDispatcherTimer DispatcherTimer { get; set; }

    public void CreateTimer(TimeSpan interval)
    {
        DispatcherTimer = Application.Current.Dispatcher.CreateTimer();
        DispatcherTimer.Interval = interval;
    }

    public void AddEventHandler(EventHandler eventHandler)
    {
        DispatcherTimer.Tick += eventHandler;
    }

    public void StopTimer()
    {
        DispatcherTimer.Stop();
    }
    public void StartTimer()
    {
        DispatcherTimer.Start();
    }

    public void RemoveEventHandler(EventHandler eventHandler)
    {
        DispatcherTimer.Tick -= eventHandler;
    }

    public void AddEventHandlerInMainThread(EventHandler eventHandler)
    {
        DispatcherTimer.Tick += (sender, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                eventHandler(sender, e);
            });
        };
    }

    public void RemoveAllEventHandlers()
    {
        DispatcherTimer = Application.Current.Dispatcher.CreateTimer();
    }
}
