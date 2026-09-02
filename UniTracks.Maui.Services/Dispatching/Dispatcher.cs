namespace UniTracks.Maui.Services.Dispatching;

public class Dispatcher : UniTracks.Services.Dispatching.IDispatcher
{
    private IDispatcherTimer? dispatcherTimer;

    public IDispatcherTimer DispatcherTimer
    {
        get => dispatcherTimer ?? throw new InvalidOperationException("CreateTimer must be called before accessing the timer.");
        set => dispatcherTimer = value;
    }

    public void CreateTimer(TimeSpan interval)
    {
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("No application dispatcher available.");
        var timer = dispatcher.CreateTimer();
        timer.Interval = interval;
        DispatcherTimer = timer;
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
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("No application dispatcher available.");
        DispatcherTimer = dispatcher.CreateTimer();
    }
}
