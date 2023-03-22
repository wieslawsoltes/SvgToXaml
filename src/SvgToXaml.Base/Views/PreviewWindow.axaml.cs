using Avalonia;
using Avalonia.Controls;

namespace SvgToXaml.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }
}
