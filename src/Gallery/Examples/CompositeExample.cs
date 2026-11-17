namespace ApexUI.Gallery.Examples;

/// Demonstrates Dropdown, Tooltip, Dialog, ContextMenu and MenuButton.
public class CompositeExample : Widget
{
    public CompositeExample()
    {
        // ── Dropdown ──────────────────────────────────────────────────────────
        var selectedFruit = new Bindable<string>("apple");

        var dropdown = new Dropdown<string>()
            .WithPlaceholder("Pick a fruit…")
            .AddItem("apple",  "Apple")
            .AddItem("banana", "Banana")
            .AddItem("cherry", "Cherry")
            .AddItem("durian", "Durian")
            .Bind(selectedFruit);

        var dropdownStatus = new Label { Text = "Selected: apple" };
        selectedFruit.Changed += v => dropdownStatus.Text = $"Selected: {v}";

        // ── Tooltip ───────────────────────────────────────────────────────────
        var tooltipBtn = new Tooltip(
            new Button("Hover me").WithVariant(ButtonVariant.Secondary),
            "Tooltip appears after 0.6 s of hovering.");

        var tooltipAbove = new Tooltip(
            new Button("Hover (above)").WithVariant(ButtonVariant.Secondary),
            "Tooltip above the button.")
            { Position = TooltipPosition.Above };

        // ── Dialog ────────────────────────────────────────────────────────────
        var alertDialog = Dialog.Alert("Alert", "This is a simple alert dialog.");

        var confirmResult = new Label { Text = "Confirm: (not yet shown)" };
        var confirmDialog = Dialog.Confirm(
            "Confirm Action",
            "Are you sure you want to delete this item?",
            onConfirm: () => confirmResult.Text = "Confirm: OK pressed",
            onCancel:  () => confirmResult.Text = "Confirm: Cancel pressed",
            confirmText: "Delete",
            cancelText:  "Cancel");

        var nameInput = new TextInput("Enter name…");
        var customResult = new Label { Text = "Custom: (not yet submitted)" };
        Dialog? customDialog = null;
        customDialog = new Dialog("Rename", nameInput)
            .AddButton("Cancel", () => customDialog!.Close())
            .AddPrimaryButton("Rename", () =>
            {
                customResult.Text = $"Custom: renamed to \"{nameInput.Value}\"";
                customDialog!.Close();
            });

        // ── ContextMenu ───────────────────────────────────────────────────────
        var bold     = new Bindable<bool>(false);
        var muted    = new Bindable<bool>(false);
        var ctxLog   = new Label { Text = "Right-click anywhere in this box" };

        // Keep ctxLog visuals in sync with the toggle bindings
        void SyncLabel()
        {
            ctxLog.Bold = bold.Value;
            ctxLog.Color = muted.Value ? new SKColor(0x88, 0x88, 0x88) : (SKColor?)null;
        }
        bold.Changed  += _ => SyncLabel();
        muted.Changed += _ => SyncLabel();

        var contextPanel = new PaddingBox(
            new Column(
                new Label { Text = "Context Menu target", Bold = true, IsHitTestVisible = false },
                ctxLog
            ).WithSpacing(4f),
            16f)
        {
            BackgroundSource = t => t.SurfaceHover,
            CornerRadius = 8f,
        };

        var menu = new ContextMenu()
            .AddHeader("Format")
            .AddCheckItem("Bold text",  bold)
            .AddCheckItem("Muted text", muted)
            .AddSeparator()
            .AddHeader("Actions")
            .AddItem("Reset label",  () => { bold.Value = false; muted.Value = false; })
            .AddItem("Copy text",    () => ctxLog.Text = "[copied]")
            .AddItem("Disabled item",() => { }, enabled: false);

        ContextMenu.Attach(contextPanel, menu);

        // ── MenuButton ────────────────────────────────────────────────────────
        var menuLog = new Label { Text = "Nothing picked yet", VAlign = VAlign.Center, ColorSource = t => t.OnSurfaceMuted };
        // IsSelected latches the tool that stays chosen: the pen, or the shape menu once a shape is picked.
        var pen    = new IconButton(new Icon("M4 20l3.5-1L19 7.5 16.5 5 5 16.5z", IconStyle.Stroke));
        var shapes = new MenuButton(new Icon("M4 4h16v16H4z", IconStyle.Stroke));
        void Pick(string tool)
        {
            pen.IsSelected    = tool == "Pen";
            shapes.IsSelected = tool != "Pen";
            menuLog.Text      = tool;
        }
        pen.OnPress(() => Pick("Pen"));
        shapes
            .AddItem("Rectangle", () => Pick("Rectangle"))
            .AddItem("Ellipse",   () => Pick("Ellipse"))
            .AddSeparator()
            .AddItem("Star",      () => { }, enabled: false);
        var file = new MenuButton("File")
            .AddItem("New",  () => menuLog.Text = "New")
            .AddItem("Open", () => menuLog.Text = "Open");

        // ── Main layout ───────────────────────────────────────────────────────
        AddChild(new PaddingBox(
            new Column(

                new Label { Text = "Dropdown", Bold = true, Role = TextRole.Heading },
                new Row(dropdown, dropdownStatus) { Spacing = 16f },

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "Tooltip", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Hover either button to see a tooltip (0.6 s delay)." },
                new Row(tooltipBtn, tooltipAbove) { Spacing = 12f },

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "Dialog / Modal", Bold = true, Role = TextRole.Heading },
                new Row(
                    new Button("Alert").OnPress(() => alertDialog.Show(Host!)),
                    new Button("Confirm").OnPress(() => confirmDialog.Show(Host!)),
                    new Button("Custom").OnPress(() => customDialog.Show(Host!))
                ) { Spacing = 8f },
                confirmResult,
                customResult,

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "ContextMenu", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Right-click the box below. Toggle items stay checked across opens." },
                contextPanel,

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "MenuButton", Bold = true, Role = TextRole.Heading },
                new Label { Text = "Click a button, or focus it and press Down, to open its menu. The pen and a picked shape stay selected." },
                new Row(new Tooltip(pen, "Pen"), new Tooltip(shapes, "Shapes"), file, menuLog) { Spacing = 8f }

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }
}
