namespace ApexUI.Core;

/// How a held key repeats: the wait before the first repeat, then the time between repeats.
public readonly record struct KeyRepeat(TimeSpan Delay, TimeSpan Interval)
{
    /// Used when the platform cannot read the system's settings: 500 ms, then 30 repeats per second.
    public static KeyRepeat Default => new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1.0 / 30));
}
