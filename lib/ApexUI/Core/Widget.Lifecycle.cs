namespace ApexUI.Core;

public abstract partial class Widget
{
    private List<Subscription>? _subscriptions;

    /// The host showing this widget, or null while it is not on screen.
    public UiHost? Host { get; private set; }

    /// Called when the widget (or an ancestor) is added to a tree that a host shows. Host is already set.
    protected virtual void OnAttached(UiHost host) { }

    /// Called when the widget leaves the host's tree. Host is still set; release resources here.
    protected virtual void OnDetached() { }

    /// Calls onChanged whenever source changes, but only while the widget is on screen or has never been shown:
    /// detaching unsubscribes, so a removed widget does not stay reachable from the bindable.
    /// Re-attaching subscribes again and calls onChanged with the current value.
    /// Dispose the result to stop observing for good (e.g. when binding to a different source).
    protected IDisposable Observe<T>(Bindable<T> source, Action<T> onChanged)
        => Track(new Subscription(this,
            () => source.Changed += onChanged,
            () => source.Changed -= onChanged,
            () => onChanged(source.Value)));

    /// Same as the Bindable overload, for list changes.
    protected IDisposable Observe<T>(ObservableList<T> source, Action onChanged)
        => Track(new Subscription(this,
            () => source.Changed += onChanged,
            () => source.Changed -= onChanged,
            onChanged));

    private Subscription Track(Subscription subscription)
    {
        (_subscriptions ??= []).Add(subscription);
        subscription.Connect(resync: false);
        return subscription;
    }

    internal void AttachToHost(UiHost host)
    {
        if (Host == host) return;
        if (Host is not null) DetachFromHost();

        Host = host;
        // Measurements taken without this host used its default font and theme.
        _measureDirty = _arrangeDirty = true;
        if (_subscriptions is not null)
            foreach (var s in _subscriptions) s.Connect(resync: true);
        OnAttached(host);
        for (int i = 0; i < _children.Count; i++)
            _children[i].AttachToHost(host);
    }

    internal void DetachFromHost()
    {
        var host = Host;
        if (host is null) return;

        for (int i = _children.Count - 1; i >= 0; i--)
            _children[i].DetachFromHost();
        OnDetached();
        if (_subscriptions is not null)
            foreach (var s in _subscriptions) s.Disconnect();
        host.Forget(this);
        Host = null;
    }

    private sealed class Subscription(Widget owner, Action subscribe, Action unsubscribe, Action pushCurrent) : IDisposable
    {
        private bool _connected;

        public void Connect(bool resync)
        {
            if (_connected) return;
            subscribe();
            _connected = true;
            if (resync) pushCurrent();
        }

        public void Disconnect()
        {
            if (!_connected) return;
            unsubscribe();
            _connected = false;
        }

        public void Dispose()
        {
            Disconnect();
            owner._subscriptions?.Remove(this);
        }
    }
}
