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
