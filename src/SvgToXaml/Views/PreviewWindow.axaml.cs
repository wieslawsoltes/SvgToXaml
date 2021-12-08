using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SvgToXaml.Views;

public class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}