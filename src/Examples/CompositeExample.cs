namespace ApexUI.App.Examples;

/// Demonstrates Dropdown, Tooltip, Dialog, and ContextMenu.
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

        // ── Main layout ───────────────────────────────────────────────────────
        AddChild(new PaddingBox(
            new Column(

                new Label { Text = "Dropdown", Bold = true, FontSize = 18f },
                new Row(dropdown, dropdownStatus) { Spacing = 16f },

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "Tooltip", Bold = true, FontSize = 18f },
                new Label { Text = "Hover either button to see a tooltip (0.6 s delay)." },
                new Row(tooltipBtn, tooltipAbove) { Spacing = 12f },

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "Dialog / Modal", Bold = true, FontSize = 18f },
                new Row(
                    new Button("Alert").OnPress(() => alertDialog.Show()),
                    new Button("Confirm").OnPress(() => confirmDialog.Show()),
                    new Button("Custom").OnPress(() => customDialog.Show())
                ) { Spacing = 8f },
                confirmResult,
                customResult,

                new Separator { Margin = new Thickness(0, 4f) },

                new Label { Text = "ContextMenu", Bold = true, FontSize = 18f },
                new Label { Text = "Right-click the box below. Toggle items stay checked across opens." },
                contextPanel

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }
}
