using ApexUI.Desktop;
using ApexUI.Gallery.Examples;

SolAndLunaTheme.Register();

var example  = new TabsExample();
var platform = new DesktopPlatform("ApexUI Demo", 900, 700).WithIcon("res/ApexIcon.svg");
new UiHost(platform)
    .BindFontFamily(example.FontFamily)
    .BindUiScale(example.Scale)
    .BindTheme(example.Preset)
    .BindDarkMode(example.DarkMode)
    .Run(example);
