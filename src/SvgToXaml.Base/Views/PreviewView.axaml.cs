using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SvgToXaml.ViewModels;

namespace SvgToXaml.Views;

public partial class PreviewView : UserControl
{
    public PreviewView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is MainWindowViewModel vm && vm.Project.SelectedItem is { })
        {
            Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var previewItem = await vm.GetPreview(vm.Project.SelectedItem);
                    if (previewItem is { })
                    {
                        var content = AvaloniaRuntimeXamlLoader.Parse<TabControl>(previewItem.TabControl);

                        content.DataContext = previewItem.Image;

                        Content = content;
                    }
                });
            });
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        Content = null;
    }
}
