using ApexUI.Gallery.Examples;

SolAndLunaTheme.Register();

var example = new TabsExample();
new Application("ApexUI Demo", 900, 700)
    .SetIcon("res/ApexIcon.svg")
    .BindFontFamily(example.FontFamily)
    .BindUiScale(example.Scale)
    .BindTheme(example.Preset)
    .BindDarkMode(example.DarkMode)
    .Run(example);
