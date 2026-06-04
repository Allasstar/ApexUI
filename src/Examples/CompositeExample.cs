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
            "This tooltip appears after 0.6 s of hovering.");

        var tooltipAbove = new Tooltip(
            new Button("Hover (above)").WithVariant(ButtonVariant.Secondary),
            "Tooltip above the button.")
            { Position = TooltipPosition.Above };

        // ── Dialog ────────────────────────────────────────────────────────────
        var alertDialog = Dialog.Alert(
            "Alert",
            "This is a simple alert dialog.\nPress OK to close.");

        var confirmResult = new Label { Text = "Confirm: (not yet shown)" };

        var confirmDialog = Dialog.Confirm(
            "Confirm Action",
            "Are you sure you want to delete this item?",
            onConfirm: () => confirmResult.Text = "Confirm: OK pressed",
            onCancel:  () => confirmResult.Text = "Confirm: Cancel pressed",
            confirmText: "Delete",
            cancelText:  "Cancel");

        // Custom dialog
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
        var ctxLabel = new Label { Text = "Right-click status: (none)" };

        var contextPanel = new PaddingBox(
            new Column(
                new Label { Text = "Right-click anywhere in this box", Bold = true },
                ctxLabel
            ).WithSpacing(4f),
            16f)
        {
            BackgroundSource = t => t.SurfaceHover,
            CornerRadius = 8f,
        };

        var menu = new ContextMenu()
            .AddHeader("Actions")
            .AddItem("Copy",   () => ctxLabel.Text = "Right-click status: Copy")
            .AddItem("Cut",    () => ctxLabel.Text = "Right-click status: Cut")
            .AddItem("Paste",  () => ctxLabel.Text = "Right-click status: Paste")
            .AddSeparator()
            .AddItem("Delete (disabled)", () => { }, enabled: false);

        ContextMenu.Attach(contextPanel, menu);

        // ── Main layout ───────────────────────────────────────────────────────
        AddChild(new PaddingBox(
            new Column(

                // Dropdown
                new Label { Text = "Dropdown", Bold = true, FontSize = 18f },
                new Row(dropdown, dropdownStatus) { Spacing = 16f },

                new Separator { Margin = new Thickness(0, 4f) },

                // Tooltip
                new Label { Text = "Tooltip", Bold = true, FontSize = 18f },
                new Label { Text = "Hover either button to see a tooltip (0.6 s delay)." },
                new Row(tooltipBtn, tooltipAbove) { Spacing = 12f },

                new Separator { Margin = new Thickness(0, 4f) },

                // Dialog
                new Label { Text = "Dialog / Modal", Bold = true, FontSize = 18f },
                new Row(
                    new Button("Alert").OnPress(() => alertDialog.Show()),
                    new Button("Confirm").OnPress(() => confirmDialog.Show()),
                    new Button("Custom").OnPress(() => customDialog.Show())
                ) { Spacing = 8f },
                confirmResult,
                customResult,

                new Separator { Margin = new Thickness(0, 4f) },

                // ContextMenu
                new Label { Text = "ContextMenu", Bold = true, FontSize = 18f },
                contextPanel

            ).WithSpacing(16f),
            new Thickness(24f)
        ));
    }
}
