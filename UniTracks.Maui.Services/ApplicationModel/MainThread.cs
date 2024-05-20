using UniTracks.Services.ApplicationModel;
using MauiMainThread = Microsoft.Maui.ApplicationModel.MainThread;

namespace UniTracks.Maui.Services.ApplicationModel;

public class MainThread : IMainThread
{
    public bool IsMainThread => MauiMainThread.IsMainThread;

    public void BeginInvokeOnMainThread(Action action)
    {
        MauiMainThread.BeginInvokeOnMainThread(action);
    }

    public async Task InvokeOnMainThreadAsync(Action action)
    {
        await MauiMainThread.InvokeOnMainThreadAsync(action);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
    {
        return await MauiMainThread.InvokeOnMainThreadAsync(func);
    }

    public async Task InvokeOnMainThreadAsync(Func<Task> func)
    {
        await MauiMainThread.InvokeOnMainThreadAsync(func);
    }

    public async Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func)
    {
        return await MauiMainThread.InvokeOnMainThreadAsync(func);
    }
}
