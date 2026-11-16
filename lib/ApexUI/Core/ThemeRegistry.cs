namespace ApexUI.Core;

/// Named theme pairs (a light and a dark variant each), in registration order.
/// Every UiHost owns one (UiHost.Themes), filled with BuiltInThemes; BindTheme picks from it.
/// Register custom themes before building UI that lists Names:
///
///   host.Themes.Register("Neon", neonLight, neonDark);
public sealed class ThemeRegistry
{
    private readonly List<string> _order = [];
    private readonly Dictionary<string, (Theme Light, Theme Dark)> _themes = new(StringComparer.OrdinalIgnoreCase);

    /// Starts with BuiltInThemes unless includeBuiltIns is false.
    public ThemeRegistry(bool includeBuiltIns = true)
    {
        if (includeBuiltIns) BuiltInThemes.RegisterAll(this);
    }

    public IReadOnlyList<string> Names => _order;

    /// The first registered name, used when a requested name is not registered.
    public string DefaultName => _order.Count > 0 ? _order[0] : BuiltInThemes.Default;

    /// Adds a theme pair, or replaces the pair registered under name (keeping its position).
    public void Register(string name, Theme light, Theme dark)
    {
        if (!_themes.ContainsKey(name)) _order.Add(name);
        _themes[name] = (light, dark);
    }

    public void Remove(string name)
    {
        if (_themes.Remove(name)) _order.Remove(name);
    }

    /// The light or dark variant of name; the first registered theme when name is unknown,
    /// Theme.Light / Theme.Dark when nothing is registered.
    public Theme Get(string name, bool dark)
    {
        if (_themes.TryGetValue(name, out var pair) || (_order.Count > 0 && _themes.TryGetValue(_order[0], out pair)))
            return dark ? pair.Dark : pair.Light;
        return dark ? Theme.Dark : Theme.Light;
    }
}
