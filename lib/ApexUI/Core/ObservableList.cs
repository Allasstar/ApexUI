namespace ApexUI.Core;

/// Mutable list that fires Changed after any structural modification.
public class ObservableList<T> : IReadOnlyList<T>
{
    private readonly List<T> _items;

    public ObservableList()                          => _items = [];
    public ObservableList(IEnumerable<T> items)      => _items = new List<T>(items);

    public T    this[int i] => _items[i];
    public int  Count       => _items.Count;
    public bool Contains(T item) => _items.Contains(item);
    public int  IndexOf(T item)  => _items.IndexOf(item);

    public IEnumerator<T> GetEnumerator()
        => _items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => _items.GetEnumerator();

    public event Action? Changed;

    public void Add(T item)                    { _items.Add(item);                Changed?.Invoke(); }
    public void Insert(int i, T item)          { _items.Insert(i, item);          Changed?.Invoke(); }
    public bool Remove(T item)                 { bool r = _items.Remove(item); if (r) Changed?.Invoke(); return r; }
    public void RemoveAt(int i)                { _items.RemoveAt(i);              Changed?.Invoke(); }
    public void Set(int i, T item)             { _items[i] = item;                Changed?.Invoke(); }
    public void AddRange(IEnumerable<T> items) { _items.AddRange(items);          Changed?.Invoke(); }
    public void Clear()                        { _items.Clear();                  Changed?.Invoke(); }
}
