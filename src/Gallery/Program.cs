using ApexUI.Desktop;
using ApexUI.Gallery.Examples;

var platform = new DesktopPlatform("ApexUI Demo", 900, 700).WithIcon("res/ApexIcon.svg");
// Keeps the tab bar and the widest examples usable.
platform.SetMinimumSize(640, 480);
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
