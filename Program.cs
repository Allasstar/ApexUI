using ApexUI.App.Examples;

var example = new TabsExample();
new Application("ApexUI Demo", 900, 700)
    .SetIcon("res/ApexIcon.svg")
    .BindUiScale(example.Scale)
    .BindDarkMode(example.DarkMode)
    .Run(example);
