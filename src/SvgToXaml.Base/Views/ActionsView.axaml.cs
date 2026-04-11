using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace SvgToXaml.Views;

public partial class ActionsView : UserControl
{
    public ActionsView()
    {
        InitializeComponent();
        InitializeThemes();
    }

    private void InitializeThemes()
    {
        var dark = true;

        ThemeButton.Click += (_, _) =>
        {
            dark = !dark;
            if (Application.Current is { } application)
            {
                application.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        };
    }
}
