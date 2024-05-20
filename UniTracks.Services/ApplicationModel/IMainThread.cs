namespace UniTracks.Services.ApplicationModel;

/// <summary>
/// Represents the main thread in an application.
/// </summary>
public interface IMainThread
{
    /// <summary>
    /// Gets a value indicating whether the current thread is the main thread.
    /// </summary>
    bool IsMainThread { get; }

    /// <summary>
    /// Executes the specified action on the main thread asynchronously.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    void BeginInvokeOnMainThread(Action action);

    /// <summary>
    /// Executes the specified action on the main thread asynchronously and returns a task representing the operation.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeOnMainThreadAsync(Action action);

    /// <summary>
    /// Executes the specified function on the main thread asynchronously and returns a task representing the operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation and containing the result of the function.</returns>
    Task<T> InvokeOnMainThreadAsync<T>(Func<T> func);

    /// <summary>
    /// Executes the specified function on the main thread asynchronously and returns a task representing the operation.
    /// </summary>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeOnMainThreadAsync(Func<Task> func);

    /// <summary>
    /// Executes the specified function on the main thread asynchronously and returns a task representing the operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <returns>A task representing the asynchronous operation and containing the result of the function.</returns>
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func);
}
