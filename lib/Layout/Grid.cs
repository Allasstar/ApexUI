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
///
/// Usage:
///   new Grid()
///       .DefineColumns(GridLength.Auto, GridLength.Star())
///       .WithSpacing(12f, 8f)
///       .Add(new Label { Text = "Name" }, 0, 0)
///       .Add(new TextInput(), 1, 0)
public class Grid : Widget
{
    private GridLength[]  _columns = [GridLength.Auto];
    private GridLength[]? _rows;

    private readonly record struct CellInfo(Widget Child, int Col, int Row, int ColSpan, int RowSpan);
    private readonly List<CellInfo> _cells = [];

    public float ColumnSpacing { get; set { field = value; InvalidateLayout(); } }
    public float RowSpacing    { get; set { field = value; InvalidateLayout(); } }

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

    public Grid WithSpacing(float columnSpacing, float rowSpacing)
    {
        ColumnSpacing = columnSpacing;
        RowSpacing    = rowSpacing;
        return this;
    }

    /// Add a child at a specific grid position. colSpan/rowSpan default to 1.
    public Grid Add(Widget child, int col = 0, int row = 0, int colSpan = 1, int rowSpan = 1)
    {
        _cells.Add(new CellInfo(child, col, row, colSpan, rowSpan));
        AddChild(child);
        return this;
    }

    protected override Size MeasureCore(Size available)
    {
        var (colW, rowH) = Resolve(available);
        float w = Sum(colW) + Math.Max(0, colW.Length - 1) * ColumnSpacing;
        float h = Sum(rowH) + Math.Max(0, rowH.Length - 1) * RowSpacing;
        return new Size(w, h);
    }

    protected override void ArrangeCore(Rect rect)
    {
        var (colW, rowH) = Resolve(new Size(rect.Width, rect.Height));

        var xOff = new float[colW.Length];
        if (xOff.Length > 0) xOff[0] = rect.X;
        for (int c = 1; c < colW.Length; c++)
            xOff[c] = xOff[c - 1] + colW[c - 1] + ColumnSpacing;

        var yOff = new float[rowH.Length];
        if (yOff.Length > 0) yOff[0] = rect.Y;
        for (int r = 1; r < rowH.Length; r++)
            yOff[r] = yOff[r - 1] + rowH[r - 1] + RowSpacing;

        foreach (var cell in _cells)
        {
            if (cell.Col >= colW.Length || cell.Row >= rowH.Length) continue;

            float cw = 0f;
            for (int c = cell.Col; c < cell.Col + cell.ColSpan && c < colW.Length; c++)
            {
                cw += colW[c];
                if (c > cell.Col) cw += ColumnSpacing;
            }
            float rh = 0f;
            for (int r = cell.Row; r < cell.Row + cell.RowSpan && r < rowH.Length; r++)
            {
                rh += rowH[r];
                if (r > cell.Row) rh += RowSpacing;
            }

            cell.Child.Measure(new Size(cw, rh));
            cell.Child.Arrange(new Rect(xOff[cell.Col], yOff[cell.Row], cw, rh));
        }
    }

    // Resolves all column widths then all row heights.
    // Column pass runs first so auto-row measurement can use resolved col widths.
    private (float[] cols, float[] rows) Resolve(Size available)
    {
        // ── Columns ────────────────────────────────────────────────────────────
        var colW = new float[_columns.Length];
        float fixedColW = 0f, starColFactor = 0f;

        for (int c = 0; c < _columns.Length; c++)
        {
            if (_columns[c].Unit == GridUnit.Fixed)
            { colW[c] = _columns[c].Value; fixedColW += colW[c]; }
            else if (_columns[c].Unit == GridUnit.Star)
                starColFactor += _columns[c].Value;
        }

        // Auto columns: measure only single-column cells to avoid span contamination
        for (int c = 0; c < _columns.Length; c++)
        {
            if (_columns[c].Unit != GridUnit.Auto) continue;
            float maxW = 0f;
            foreach (var cell in _cells)
                if (cell.Col == c && cell.ColSpan == 1)
                {
                    cell.Child.Measure(new Size(float.PositiveInfinity, float.PositiveInfinity));
                    maxW = Math.Max(maxW, cell.Child.DesiredSize.Width);
                }
            colW[c] = maxW;
            fixedColW += maxW;
        }

        float colSpacingTotal = (_columns.Length > 1 ? _columns.Length - 1 : 0) * ColumnSpacing;
        float starColAvail    = Math.Max(0f, available.Width - fixedColW - colSpacingTotal);
        for (int c = 0; c < _columns.Length; c++)
            if (_columns[c].Unit == GridUnit.Star)
                colW[c] = starColFactor > 0f ? starColAvail * (_columns[c].Value / starColFactor) : 0f;

        // ── Rows ───────────────────────────────────────────────────────────────
        var resolvedRows = _rows ?? BuildAutoRows();
        var rowH = new float[resolvedRows.Length];
        float fixedRowH = 0f, starRowFactor = 0f;

        for (int r = 0; r < resolvedRows.Length; r++)
        {
            if (resolvedRows[r].Unit == GridUnit.Fixed)
            { rowH[r] = resolvedRows[r].Value; fixedRowH += rowH[r]; }
            else if (resolvedRows[r].Unit == GridUnit.Star)
                starRowFactor += resolvedRows[r].Value;
        }

        // Auto rows: measure single-row cells using their resolved span width
        for (int r = 0; r < resolvedRows.Length; r++)
        {
            if (resolvedRows[r].Unit != GridUnit.Auto) continue;
            float maxH = 0f;
            foreach (var cell in _cells)
                if (cell.Row == r && cell.RowSpan == 1)
                {
                    float cw = 0f;
                    for (int c = cell.Col; c < cell.Col + cell.ColSpan && c < colW.Length; c++)
                    {
                        cw += colW[c];
                        if (c > cell.Col) cw += ColumnSpacing;
                    }
                    cell.Child.Measure(new Size(Math.Max(cw, 0f), float.PositiveInfinity));
                    maxH = Math.Max(maxH, cell.Child.DesiredSize.Height);
                }
            rowH[r] = maxH;
            fixedRowH += maxH;
        }

        float rowSpacingTotal = (resolvedRows.Length > 1 ? resolvedRows.Length - 1 : 0) * RowSpacing;
        float starRowAvail    = Math.Max(0f, available.Height - fixedRowH - rowSpacingTotal);
        for (int r = 0; r < resolvedRows.Length; r++)
            if (resolvedRows[r].Unit == GridUnit.Star)
                rowH[r] = starRowFactor > 0f ? starRowAvail * (resolvedRows[r].Value / starRowFactor) : 0f;

        return (colW, rowH);
    }

    // When DefineRows() is not called, infer one Auto row per unique row index in _cells.
    private GridLength[] BuildAutoRows()
    {
        int maxRow = 0;
        foreach (var cell in _cells)
            maxRow = Math.Max(maxRow, cell.Row + cell.RowSpan);
        var arr = new GridLength[maxRow];
        for (int i = 0; i < maxRow; i++) arr[i] = GridLength.Auto;
        return arr;
    }

    private static float Sum(float[] arr)
    {
        float s = 0f;
        foreach (var v in arr) s += v;
        return s;
    }
}
