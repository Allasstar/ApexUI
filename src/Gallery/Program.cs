using ApexUI.Desktop;
using ApexUI.Gallery.Examples;

var platform = new DesktopPlatform("ApexUI Demo", 900, 700).WithIcon("res/ApexIcon.svg");
var host     = new UiHost(platform);
// Registered before the Settings tab lists the theme names.
SolAndLunaTheme.Register(host.Themes);

var example = new TabsExample(host.Themes);
host.BindFontFamily(example.FontFamily)
    .BindUiScale(example.Scale)
    .BindResizeFrameRate(example.ResizeFrameRate)
    .BindTheme(example.Preset)
    .BindDarkMode(example.DarkMode)
    .Run(example);
