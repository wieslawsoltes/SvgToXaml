using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SvgToXaml.Views;

public class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        InitializeThemes();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeThemes()
    {
        var dark = true;
        var theme = this.Find<Button>("Theme");
        if (theme is { })
        {
            theme.Click += (_, _) =>
            {
                dark = !dark;
                App.ThemeManager?.Switch(dark ? 1 : 0);
            };
        }
    }
}
