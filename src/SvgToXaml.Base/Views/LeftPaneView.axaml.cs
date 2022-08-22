using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SvgToXaml.Views;

public partial class LeftPane : UserControl
{
    public LeftPane()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

