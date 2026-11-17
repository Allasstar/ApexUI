namespace ApexUI.Widgets;

/// Text that turns into a text field on double-click (or BeginEdit) for renaming in place.
/// Enter or moving focus away commits; Escape cancels. A commit that leaves the text unchanged or
/// blank cancels too. TextCommitted reports the new text after Text is updated.
public class EditableLabel : Widget
{
    private readonly Label _label;
    private readonly TextInput _input;

    public EditableLabel(string text = "")
    {
        _label = new Label { Text = text };
        _input = new TextInput { IsVisible = false };
        _input.Submitted += _ => Commit();
        _input.LostFocus += Commit;
        AddChild(_label);
        AddChild(_input);
    }

    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    /// The label inside, for styling (role, bold, color).
    public Label Label => _label;

    public bool IsEditing => _input.IsVisible;

    public event Action<string>? TextCommitted;

    public EditableLabel OnCommit(Action<string> action) { TextCommitted += action; return this; }

    public void BeginEdit()
    {
        if (IsEditing) return;
        _input.Value = Text;
        _label.IsVisible = false;
        _input.IsVisible = true;
        _input.Focus();
        _input.SelectAll();
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (IsEditing || e.Button != PointerButton.Left || e.ClickCount != 2) return;
        e.Handled = true;
        BeginEdit();
    }

    protected override void OnDetached()
    {
        if (IsEditing) EndEdit();
    }

    private void Commit()
    {
        if (!IsEditing) return;

        string text = _input.Value.Trim();
        EndEdit();
        if (text.Length == 0 || text == Text) return;

        Text = text;
        TextCommitted?.Invoke(text);
    }

    private void EndEdit()
    {
        _input.IsVisible = false;
        _label.IsVisible = true;
    }
}
