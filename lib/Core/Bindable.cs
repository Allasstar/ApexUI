namespace ApexUI.Core;

/// Observable value holder for two-way widget binding.
/// Setting Value with the same value is a no-op (equality guard).
/// Re-entrant sets during notification are suppressed to prevent binding cycles.
public class Bindable<T>
{
    private T    _value;
    private bool _notifying;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            if (_notifying) return;
            _notifying = true;
            try   { Changed?.Invoke(value); }
            finally { _notifying = false; }
        }
    }

    public event Action<T>? Changed;

    public Bindable(T initial = default!) => _value = initial;
}
