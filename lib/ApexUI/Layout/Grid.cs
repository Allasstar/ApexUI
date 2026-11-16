namespace ApexUI.Layout;

public enum GridUnit { Auto, Fixed, Star }

/// Column or row size definition for Grid.
public readonly record struct GridLength(float Value, GridUnit Unit)
{
    public static readonly GridLength Auto = new(0f, GridUnit.Auto);
    public static GridLength Fixed(float px) => new(px, GridUnit.Fixed);
    public static GridLength Star(float factor = 1f) => new(factor, GridUnit.Star);
}

/// 2D layout panel with explicit column/row definitions.
/// Children are placed by (col, row) coordinate via Add(). Supports column/row spanning,
/// Auto sizing (max of cell natural sizes), Fixed px sizes, and Star (*) fill sizing.
/// Star tracks size like Auto when the constraint along them is infinite.
///
/// Usage:
///   new Grid()
///       .DefineColumns(GridLength.Auto, GridLength.Star())
///       .WithSpacing(12f, 8f)
///       .Add(new Label { Text = "Name" }, 0, 0)
///       .Add(new TextInput(), 1, 0)
public class Grid : Widget
{
    private sealed class Cell(int col, int row, int colSpan, int rowSpan)
    {
        public readonly int Col = col, Row = row, ColSpan = colSpan, RowSpan = rowSpan;
    }

    private GridLength[]  _columns = [GridLength.Auto];
    private GridLength[]? _rows;

    // Track sizes and offsets, reused across layout passes.
    private float[] _colW = [];
    private float[] _rowH = [];
    private float[] _colX = [];
    private float[] _rowY = [];
    private int _rowCount;

    public float ColumnSpacing { get; set => SetLayoutProperty(ref field, value); }
    public float RowSpacing    { get; set => SetLayoutProperty(ref field, value); }

    public Grid DefineColumns(params GridLength[] columns)
    {
        _columns = columns;
        InvalidateLayout();
        return this;
    }

    public Grid DefineRows(params GridLength[] rows)
    {
        _rows = rows;
        InvalidateLayout();
        return this;
    }

    /// Shorthand for N equal Star(*) columns (replaces any prior DefineColumns call).
    public Grid WithColumns(int count)
    {
        var cols = new GridLength[count];
        for (int i = 0; i < count; i++) cols[i] = GridLength.Star();
        return DefineColumns(cols);
    }

    /// Shorthand for N equal Star(*) rows (replaces any prior DefineRows call).
    public Grid WithRows(int count)
    {
        var rows = new GridLength[count];
        for (int i = 0; i < count; i++) rows[i] = GridLength.Star();
        return DefineRows(rows);
    }

    public Grid WithSpacing(float columnSpacing, float rowSpacing)
    {
        ColumnSpacing = columnSpacing;
        RowSpacing    = rowSpacing;
        return this;
    }

    /// Add a child at a specific grid position. colSpan/rowSpan default to 1.
    public Grid Add(Widget child, int col = 0, int row = 0, int colSpan = 1, int rowSpan = 1)
    {
        AddChild(child);
        SetParentData(child, new Cell(col, row, colSpan, rowSpan));
        return this;
    }

    protected override Size MeasureCore(Size available)
    {
        Resolve(available);
        return new Size(
            Total(_colW, _columns.Length, ColumnSpacing),
            Total(_rowH, _rowCount, RowSpacing));
    }

    protected override void ArrangeCore(Rect rect)
    {
        Resolve(new Size(rect.Width, rect.Height));
        Offsets(_colW, _colX, _columns.Length, rect.X, ColumnSpacing);
        Offsets(_rowH, _rowY, _rowCount, rect.Y, RowSpacing);

        foreach (var child in Children)
        {
            if (GetParentData(child) is not Cell cell) continue;
            // A cell outside the defined tracks gets an empty slot at the grid's origin.
            if (cell.Col >= _columns.Length || cell.Row >= _rowCount)
            {
                child.Arrange(new Rect(rect.X, rect.Y, 0f, 0f));
                continue;
            }

            float cw = SpanLength(_colW, cell.Col, cell.ColSpan, _columns.Length, ColumnSpacing);
            float rh = SpanLength(_rowH, cell.Row, cell.RowSpan, _rowCount, RowSpacing);
            child.Measure(new Size(cw, rh));
            child.Arrange(new Rect(_colX[cell.Col], _rowY[cell.Row], cw, rh));
        }
    }

    // Resolves all column widths, then all row heights; auto rows are measured at their resolved
    // column widths. Only single-span cells size Auto tracks, so spans do not inflate them.
    private void Resolve(Size available)
    {
        _rowCount = _rows?.Length ?? InferredRowCount();
        EnsureCapacity(ref _colW, ref _colX, _columns.Length);
        EnsureCapacity(ref _rowH, ref _rowY, _rowCount);

        // ── Columns ────────────────────────────────────────────────────────────
        bool starColsAsAuto = float.IsInfinity(available.Width);
        float fixedColW = 0f, starColFactor = 0f;
        for (int c = 0; c < _columns.Length; c++)
        {
            var col = _columns[c];
            _colW[c] = 0f;
            if (col.Unit == GridUnit.Fixed) { _colW[c] = col.Value; fixedColW += col.Value; }
            else if (col.Unit == GridUnit.Star && !starColsAsAuto) starColFactor += col.Value;
        }

        foreach (var child in Children)
        {
            if (GetParentData(child) is not Cell cell || cell.ColSpan != 1 || cell.Col >= _columns.Length) continue;
            if (!SizesLikeAuto(_columns[cell.Col].Unit, starColsAsAuto)) continue;
            child.Measure(Size.Infinite);
            _colW[cell.Col] = Math.Max(_colW[cell.Col], child.DesiredSize.Width);
        }
        for (int c = 0; c < _columns.Length; c++)
            if (SizesLikeAuto(_columns[c].Unit, starColsAsAuto)) fixedColW += _colW[c];

        if (!starColsAsAuto)
        {
            float starColAvail = Math.Max(0f, available.Width - fixedColW - SpacingTotal(_columns.Length, ColumnSpacing));
            for (int c = 0; c < _columns.Length; c++)
                if (_columns[c].Unit == GridUnit.Star)
                    _colW[c] = starColFactor > 0f ? starColAvail * (_columns[c].Value / starColFactor) : 0f;
        }

        // ── Rows ───────────────────────────────────────────────────────────────
        bool starRowsAsAuto = float.IsInfinity(available.Height);
        float fixedRowH = 0f, starRowFactor = 0f;
        for (int r = 0; r < _rowCount; r++)
        {
            var row = RowAt(r);
            _rowH[r] = 0f;
            if (row.Unit == GridUnit.Fixed) { _rowH[r] = row.Value; fixedRowH += row.Value; }
            else if (row.Unit == GridUnit.Star && !starRowsAsAuto) starRowFactor += row.Value;
        }

        foreach (var child in Children)
        {
            if (GetParentData(child) is not Cell cell || cell.RowSpan != 1 || cell.Row >= _rowCount) continue;
            if (!SizesLikeAuto(RowAt(cell.Row).Unit, starRowsAsAuto)) continue;
            float cw = SpanLength(_colW, cell.Col, cell.ColSpan, _columns.Length, ColumnSpacing);
            child.Measure(new Size(cw, float.PositiveInfinity));
            _rowH[cell.Row] = Math.Max(_rowH[cell.Row], child.DesiredSize.Height);
        }
        for (int r = 0; r < _rowCount; r++)
            if (SizesLikeAuto(RowAt(r).Unit, starRowsAsAuto)) fixedRowH += _rowH[r];

        if (!starRowsAsAuto)
        {
            float starRowAvail = Math.Max(0f, available.Height - fixedRowH - SpacingTotal(_rowCount, RowSpacing));
            for (int r = 0; r < _rowCount; r++)
                if (RowAt(r).Unit == GridUnit.Star)
                    _rowH[r] = starRowFactor > 0f ? starRowAvail * (RowAt(r).Value / starRowFactor) : 0f;
        }
    }

    // Without DefineRows, one Auto row per row index any cell reaches.
    private GridLength RowAt(int r) => _rows is null ? GridLength.Auto : _rows[r];

    private int InferredRowCount()
    {
        int count = 0;
        foreach (var child in Children)
            if (GetParentData(child) is Cell cell)
                count = Math.Max(count, cell.Row + cell.RowSpan);
        return count;
    }

    private static bool SizesLikeAuto(GridUnit unit, bool starAsAuto)
        => unit == GridUnit.Auto || (unit == GridUnit.Star && starAsAuto);

    private static void EnsureCapacity(ref float[] sizes, ref float[] offsets, int count)
    {
        if (sizes.Length >= count) return;
        sizes   = new float[count];
        offsets = new float[count];
    }

    private static float SpacingTotal(int count, float spacing) => Math.Max(0, count - 1) * spacing;

    private static float Total(float[] sizes, int count, float spacing)
    {
        float sum = SpacingTotal(count, spacing);
        for (int i = 0; i < count; i++) sum += sizes[i];
        return sum;
    }

    private static void Offsets(float[] sizes, float[] offsets, int count, float start, float spacing)
    {
        float pos = start;
        for (int i = 0; i < count; i++)
        {
            offsets[i] = pos;
            pos += sizes[i] + spacing;
        }
    }

    private static float SpanLength(float[] sizes, int start, int span, int count, float spacing)
    {
        float length = 0f;
        int end = Math.Min(start + span, count);
        for (int i = start; i < end; i++)
        {
            length += sizes[i];
            if (i > start) length += spacing;
        }
        return Math.Max(0f, length);
    }
}
