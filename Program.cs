using ApexUI.App.Examples;

var app     = new Application("ApexUI Demo", 900, 700) { Theme = Theme.Light };
var example = new TabsExample();
example.Scale.Changed    += v => app.UiScale = v;
example.DarkMode.Changed += v => app.Theme   = v ? Theme.Dark : Theme.Light;
app.Run(example);
