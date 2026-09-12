using System.Linq.Expressions;
using AgredoApplication.MVVM.Services.Abstractions.Application;
using AgredoApplication.MVVM.Services.Abstractions.IO;
using AgredoApplication.MVVM.Services.Abstractions.Navigation;
using UniTracks.Data.Repository;
using UniTracks.Models.GPS;
using UniTracks.Services.ApplicationModel;
using UniTracks.Services.ApplicationModel.Permissions;
using UniTracks.Services.Data;
using UniTracks.Services.Dispatching;
using UniTracks.Services.Location;
using LocationModel = UniTracks.Models.Location.Location;

namespace UniTracks.Tests.ViewModels.Fakes;

/// <summary>
/// Hand-written fakes for the ViewModel layer. <c>UniTracks.ViewModels</c> depends on nothing but
/// interfaces (it has no MAUI reference at all), so plain fakes are enough and no mocking library is
/// needed.
///
/// Deliberate design rule: every <see cref="Task"/>-returning member completes <em>synchronously</em>
/// (via <see cref="Task.FromResult{TResult}"/> / <see cref="Task.CompletedTask"/>). Both ViewModels
/// start work from their constructor with a fire-and-forget <c>_ = SomethingAsync()</c>; because an
/// <c>await</c> over an already-completed task continues inline, that work has already finished by
/// the time the constructor returns. That makes the constructor side effects deterministic instead
/// of racy. Returning <c>Task.Run(...)</c> here would silently break that.
/// </summary>
internal sealed class FakeNavigationService : INavigationService
{
    public Dictionary<string, object> Parameters { get; set; } = new();

    public int NavigateBackCalls { get; private set; }

    public List<(string Route, bool Animate, IDictionary<string, object>? Parameters)> Navigations { get; } = new();

    public Task NavigateBack()
    {
        NavigateBackCalls++;
        return Task.CompletedTask;
    }

    public Task ShellNavigationTo(string route) => Record(route, false, null);

    public Task ShellNavigationTo(string route, bool animate) => Record(route, animate, null);

    public Task ShellNavigationTo(string route, Dictionary<string, object> parameters) =>
        Record(route, false, parameters);

    public Task ShellNavigationTo(string route, bool animate, IDictionary<string, object> parameters) =>
        Record(route, animate, parameters);

    private Task Record(string route, bool animate, IDictionary<string, object>? parameters)
    {
        Navigations.Add((route, animate, parameters));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Popup fake. Popup results default to <c>default(T)</c> — i.e. "user dismissed the popup" — which
/// is the safe outcome for the ViewModels under test: <c>SearchTripType</c> only acts on a non-null
/// selection.
/// </summary>
internal sealed class FakePopupNavigationService : IPopupNavigationService
{
    public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    public string EditDocumentLabelPopup { get; set; } = string.Empty;

    public List<string> ShownPopups { get; } = new();

    /// <summary>When set, the next <c>ShowPopupAsync</c> throws it, modelling a popup that could not be presented.</summary>
    public Exception? ShowFailure { get; set; }

    public void ShowPopup(string popupName) => ShownPopups.Add(popupName);

    public void ShowPopup(string popupName, Dictionary<string, object> parameters) => ShownPopups.Add(popupName);

    public Task<TResult> ShowPopupByViewModel<TViewModel, TResult>(string titel, string description)
        where TViewModel : BasePopupViewModel
        where TResult : new()
    {
        ShownPopups.Add(typeof(TViewModel).Name);
        return Task.FromResult(default(TResult)!);
    }

    public Task<T> ShowPopup<T>(string popupName)
    {
        ShownPopups.Add(popupName);
        return Task.FromResult(default(T)!);
    }

    public Task<T> ShowPopup<T>(string popupName, Dictionary<string, object> parameters)
    {
        ShownPopups.Add(popupName);
        return Task.FromResult(default(T)!);
    }

    public Task ShowPopupAsync<TViewModel>(CancellationToken cancellationToken = default)
        where TViewModel : class
    {
        if (ShowFailure is not null)
        {
            throw ShowFailure;
        }

        ShownPopups.Add(typeof(TViewModel).Name);
        return Task.CompletedTask;
    }

    public Task<TResult?> ShowPopupAsync<TViewModel, TResult>(CancellationToken cancellationToken = default)
        where TViewModel : class
    {
        ShownPopups.Add(typeof(TViewModel).Name);
        return Task.FromResult(default(TResult));
    }

    public Task<TResult?> ShowPopupAsync<TViewModel, TResult>(
        Action<TViewModel> onViewModelCreated,
        CancellationToken cancellationToken = default)
        where TViewModel : class
    {
        ShownPopups.Add(typeof(TViewModel).Name);
        return Task.FromResult(default(TResult));
    }

    public Task<TResult?> ShowPopupAsync<TViewModel, TResult>(
        Func<TViewModel, Task> onViewModelCreated,
        CancellationToken cancellationToken = default)
        where TViewModel : class
    {
        ShownPopups.Add(typeof(TViewModel).Name);
        return Task.FromResult(default(TResult));
    }
}

internal sealed class FakeLocationService : ILocationService
{
    public int StartListeningCalls { get; private set; }

    public int StopListeningCalls { get; private set; }

    public Action<GPSInformatoion>? LastCallback { get; private set; }

    public Task StartListening(Action<GPSInformatoion> onLocationReceived)
    {
        StartListeningCalls++;
        LastCallback = onLocationReceived;
        return Task.CompletedTask;
    }

    public Task StartListening()
    {
        StartListeningCalls++;
        return Task.CompletedTask;
    }

    public void StopListening() => StopListeningCalls++;
}

/// <summary>
/// Permission fake. <see cref="Status"/> is returned for both the status check and the request, so a
/// single assignment models "user denies" or "user grants".
/// </summary>
internal sealed class FakePermissions : IPermissions
{
    public PermissionStatus Status { get; set; } = PermissionStatus.Granted;

    public bool ShouldShowRationaleResult { get; set; }

    public List<Permission> CheckedPermissions { get; } = new();

    public List<Permission> RequestedPermissions { get; } = new();

    public Task<PermissionStatus> CheckPermissionStatusAsync(Permission permission)
    {
        CheckedPermissions.Add(permission);
        return Task.FromResult(Status);
    }

    public Task<PermissionStatus> RequestPermissionAsync(Permission permission)
    {
        RequestedPermissions.Add(permission);
        return Task.FromResult(Status);
    }

    public bool ShouldShowRationale(Permission permission) => ShouldShowRationaleResult;
}

/// <summary>Runs everything inline: the ViewModels only use the main thread to update bound text.</summary>
internal sealed class FakeMainThread : IMainThread
{
    public bool IsMainThread => true;

    public void BeginInvokeOnMainThread(Action action) => action();

    public Task InvokeOnMainThreadAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => Task.FromResult(func());
}

/// <summary>
/// Timer fake. The real platform timer ticks on a background thread and the ViewModel's handler then
/// pushes the stopwatch reading to the UI; <see cref="RaiseTimerTick"/> reproduces exactly that, which
/// is the only way to observe the clock without sleeping on a real timer.
/// </summary>
internal sealed class FakeDispatcher : IDispatcher
{
    public int CreateTimerCalls { get; private set; }

    public TimeSpan? TimerInterval { get; private set; }

    public int StartTimerCalls { get; private set; }

    public int StopTimerCalls { get; private set; }

    public EventHandler? TickHandler { get; private set; }

    public void CreateTimer(TimeSpan interval)
    {
        CreateTimerCalls++;
        TimerInterval = interval;
    }

    public void AddEventHandler(EventHandler handler) => TickHandler = handler;

    public void RemoveEventHandler(EventHandler handler)
    {
        if (ReferenceEquals(TickHandler, handler))
        {
            TickHandler = null;
        }
    }

    public void AddEventHandlerInMainThread(EventHandler handler) => TickHandler = handler;

    public void RemoveAllEventHandlers() => TickHandler = null;

    public void StopTimer() => StopTimerCalls++;

    public void StartTimer() => StartTimerCalls++;

    /// <summary>Fires the captured timer callback, as the platform timer would.</summary>
    public void RaiseTimerTick() => TickHandler?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// File-system stub. The ViewModels under test only forward the app data directory, so every member
/// that would touch a real file returns an empty result instead.
/// </summary>
internal sealed class FakeFileSystem : IFileSystem
{
    public string AppDataDirectory { get; set; } = "test://app-data";

    public Task<string> PickFilePath() => Task.FromResult(string.Empty);

    public Task<string> PickDirectoryPath() => Task.FromResult(string.Empty);

    public Task<string> CopyFolderToAppDirectory(string source, string destination) => Task.FromResult(string.Empty);

    public Task<bool> StoreFile(byte[] content, string fileName, string folder) => Task.FromResult(true);

    public Task<string> EnsureAppPackageFileCopiedAsync(string fileName, string destination) =>
        Task.FromResult(string.Empty);

    public Task<string?> ReadAppPackageFileAsync(string fileName) => Task.FromResult<string?>(string.Empty);

    public Task ShareFileAsync(string fileName, string title) => Task.CompletedTask;

    public Task ShareFilesAsync(string title, IEnumerable<string> fileNames) => Task.CompletedTask;

    public Task SaveFileAsync(string fileName) => Task.CompletedTask;

    public Task SaveFilesAsync(IEnumerable<string> fileNames) => Task.CompletedTask;
}

internal sealed class FakeGpsDataStorageService : IGpsDataStorageService
{
    public Guid? CurrentTripTypeId { get; set; }

    public int FinalizeTripCalls { get; private set; }

    public List<GPSInformatoion> StoredData { get; } = new();

    public List<LocationModel> Locations { get; } = new();

    public Task StoreData(GPSInformatoion information, Action<GPSInformatoion> callback)
    {
        StoredData.Add(information);
        callback(information);
        return Task.CompletedTask;
    }

    public Task StoreData(GPSInformatoion information)
    {
        StoredData.Add(information);
        return Task.CompletedTask;
    }

    public Task<List<LocationModel>> getAll() => Task.FromResult(Locations.ToList());

    public void FinalizeTrip() => FinalizeTripCalls++;
}

/// <summary>
/// In-memory <see cref="IRepository"/>. Tables are keyed by entity type, so the same instance serves
/// the <c>User</c>, <c>TripType</c>, <c>Trip</c> and <c>Location</c> queries a ViewModel issues.
///
/// Two details matter for the tests: writes mutate the seeded instances in place (the ViewModels
/// assert on the very objects they were handed), and every write is appended to
/// <see cref="Calls"/> so call <em>order</em> can be asserted — that is what pins down finding #7,
/// where locations have to be deleted before their trip.
/// </summary>
internal sealed class InMemoryRepository : IRepository
{
    private readonly Dictionary<Type, List<object>> tables = new();
    private long dataVersion;

    public string DatabasePath => "test://unitracks-in-memory";

    /// <inheritdoc />
    public long DataVersion => dataVersion;

    public List<(string Operation, Type EntityType, IReadOnlyList<object> Entities)> Calls { get; } = new();

    /// <summary>Replaces the table for <typeparamref name="TEntity"/> with <paramref name="entities"/>.</summary>
    public void Seed<TEntity>(params TEntity[] entities)
        where TEntity : class
    {
        var table = Table<TEntity>();
        table.Clear();
        table.AddRange(entities.Cast<object>());
    }

    public IReadOnlyList<TEntity> Rows<TEntity>() => Table<TEntity>().Cast<TEntity>().ToList();

    public Task<TEntity> Add<TEntity>(TEntity entity)
        where TEntity : class
    {
        Calls.Add(("Add", typeof(TEntity), new object[] { entity! }));
        dataVersion++;
        Table<TEntity>().Add(entity!);
        return Task.FromResult(entity);
    }

    public Task<TEntity> Update<TEntity>(TEntity entity)
        where TEntity : class
    {
        Calls.Add(("Update", typeof(TEntity), new object[] { entity! }));
        dataVersion++;
        return Task.FromResult(entity);
    }

    public Task Delete<TEntity>(TEntity entity)
        where TEntity : class
    {
        dataVersion++;
        Calls.Add(("Delete", typeof(TEntity), new object[] { entity! }));
        Table<TEntity>().Remove(entity!);
        return Task.CompletedTask;
    }

    public Task DeleteRange<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        var deleted = entities.Cast<object>().ToList();
        dataVersion++;
        Calls.Add(("DeleteRange", typeof(TEntity), deleted));

        foreach (var entity in deleted)
        {
            Table<TEntity>().Remove(entity);
        }

        return Task.CompletedTask;
    }

    public Task<TEntity?> GetByIdAsync<TEntity>(Guid id)
        where TEntity : class =>
        throw new NotSupportedException("Kein ViewModel unter Test liest per ID; bitte bei Bedarf ergaenzen.");

    /// <summary>
    /// Includes are ignored: the fake stores whole object graphs, so a seeded <c>Trip</c> already
    /// carries its <c>Locations</c> the way the real include would materialise them.
    /// </summary>
    public Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] includes)
        where TEntity : class =>
        Task.FromResult<IEnumerable<TEntity>>(Table<TEntity>().Cast<TEntity>().ToList());

    public Task<IEnumerable<TEntity>> GetAsync<TEntity>(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] includes)
        where TEntity : class =>
        Task.FromResult(Get(filter, includes));

    public IEnumerable<TEntity> Get<TEntity>(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] includes)
        where TEntity : class
    {
        var rows = Table<TEntity>().Cast<TEntity>();
        return filter is null ? rows.ToList() : rows.Where(filter.Compile()).ToList();
    }

    private List<object> Table<TEntity>()
    {
        if (!tables.TryGetValue(typeof(TEntity), out var table))
        {
            table = new List<object>();
            tables[typeof(TEntity)] = table;
        }

        return table;
    }
}
