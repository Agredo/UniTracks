namespace UniTracks.Services.Comparison;

/// <summary>
/// Runs the one-time indexing of the existing trip library.
/// <para>
/// Deriving a fingerprint means reading a trip's GPS points, which is the most expensive read in the
/// app. Doing that on the compare page would make the first visit hang, so it is started once in the
/// background and the comparison simply shows whatever is indexed so far.
/// </para>
/// </summary>
public interface ITripFingerprintBackfill
{
    /// <summary>Starts the pass if it has not run in this process yet. Returns immediately.</summary>
    void EnsureStarted();

    bool IsRunning { get; }

    /// <summary>Completes when the pass that is running (or the last one) has finished.</summary>
    Task<int> WaitAsync();

    /// <summary>Raised once when a pass finishes, so an open page can refresh itself.</summary>
    event EventHandler? Completed;
}

/// <inheritdoc />
public sealed class TripFingerprintBackfill : ITripFingerprintBackfill
{
    private readonly ITripFingerprintService fingerprints;
    private readonly object gate = new();

    private Task<int>? current;

    public TripFingerprintBackfill(ITripFingerprintService fingerprints)
    {
        this.fingerprints = fingerprints;
    }

    public event EventHandler? Completed;

    public bool IsRunning
    {
        get
        {
            lock (gate)
            {
                return current is { IsCompleted: false };
            }
        }
    }

    public void EnsureStarted()
    {
        lock (gate)
        {
            if (current is not null)
            {
                return;
            }

            current = Task.Run(async () =>
            {
                try
                {
                    return await fingerprints.BackfillAsync();
                }
                catch (Exception exception)
                {
                    // Indexing is an optimisation, never a reason to take the app down. The next
                    // start tries again, and comparisons keep working on whatever is already stored.
                    System.Diagnostics.Debug.WriteLine($"[TripFingerprintBackfill] indexing failed: {exception}");
                    return 0;
                }
                finally
                {
                    Completed?.Invoke(this, EventArgs.Empty);
                }
            });
        }
    }

    public Task<int> WaitAsync()
    {
        lock (gate)
        {
            return current ?? Task.FromResult(0);
        }
    }
}
