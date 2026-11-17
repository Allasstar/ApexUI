using System.Collections.Concurrent;

namespace ApexUI.Desktop;

/// Runs posted callbacks on the UI thread between event-loop iterations, so code after an await
/// (a file dialog, file I/O) continues on the UI thread where widgets may be touched.
internal sealed class LoopSynchronizationContext(Action wake) : SynchronizationContext
{
    private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();
    private readonly int _threadId = Environment.CurrentManagedThreadId;

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue((d, state));
        wake();
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Environment.CurrentManagedThreadId == _threadId)
        {
            d(state);
            return;
        }

        using var done = new ManualResetEventSlim();
        Post(s => { try { d(s); } finally { done.Set(); } }, state);
        done.Wait();
    }

    public override SynchronizationContext CreateCopy() => this;

    public bool HasWork => !_queue.IsEmpty;

    /// Runs the callbacks queued so far; ones posted while running wait for the next call.
    public void RunPending()
    {
        for (int count = _queue.Count; count > 0 && _queue.TryDequeue(out var item); count--)
            item.Callback(item.State);
    }
}
