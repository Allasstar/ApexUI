using System.Collections;

namespace ApexUI.Core;

/// Read-only view of a widget's children. `foreach` over it uses the list's struct enumerator,
/// so walking the tree allocates nothing.
public readonly struct ChildList : IReadOnlyList<Widget>
{
    private readonly List<Widget> _list;

    internal ChildList(List<Widget> list) => _list = list;

    public int Count => _list.Count;
    public Widget this[int index] => _list[index];

    public List<Widget>.Enumerator GetEnumerator() => _list.GetEnumerator();
    IEnumerator<Widget> IEnumerable<Widget>.GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
}
