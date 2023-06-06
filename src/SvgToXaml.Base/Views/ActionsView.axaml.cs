using Avalonia.Controls;

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
            App.ThemeManager?.Switch(dark ? 1 : 0);
        };
    }
}
