using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SvgToXaml.ViewModels;

namespace SvgToXaml.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        e.DragEffects = files is { Length: > 0 }
            ? e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link)
            : DragDropEffects.None;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        var paths = e.DataTransfer.TryGetFiles()?
            .Select(x => x.Path)
            .Where(x => x.IsAbsoluteUri)
            .Select(x => x.LocalPath)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (paths is { Length: > 0 })
        {
            if (DataContext is MainWindowViewModel vm)
            {
                try
                {
                    vm.Drop(paths);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
