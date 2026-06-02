using ApexUI.App.Examples;

new Application("ApexUI Demo", 800, 600)
{
    Theme = Theme.Light   // swap to Theme.Dark for instant dark mode
}
.Run(new CounterExample());
