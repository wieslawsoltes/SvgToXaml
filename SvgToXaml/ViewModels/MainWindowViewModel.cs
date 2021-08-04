using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using ReactiveUI;
using Svg.Skia;
using SvgToXaml.Views;
using SvgToXamlConverter;

namespace SvgToXaml.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<FileItemViewModel>? _items;
        private FileItemViewModel? _selectedItem;
        private bool _enableGenerateImage;
        private bool _enableGeneratePreview;

        public FileItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ObservableCollection<FileItemViewModel>? Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        public bool EnableGenerateImage
        {
            get => _enableGenerateImage;
            set => this.RaiseAndSetIfChanged(ref _enableGenerateImage, value);
        }

        public bool EnableGeneratePreview
        {
            get => _enableGeneratePreview;
            set => this.RaiseAndSetIfChanged(ref _enableGeneratePreview, value);
        }

        public ICommand ClearCommand { get; }
        
        public ICommand AddCommand { get; }

        public ICommand CopySelectedCommand { get; }

        public ICommand CopyAllCommand { get; }

        public ICommand ExportSelectedCommand { get; }

        public ICommand ExportAllCommand { get; }

        public ICommand ClipboardCommand { get; }

        public MainWindowViewModel()
        {
            _items = new ObservableCollection<FileItemViewModel>();

            _enableGenerateImage = false;
            _enableGeneratePreview = true;

            ClearCommand = ReactiveCommand.Create(() =>
            {
                SelectedItem = null;
                _items?.Clear();
            });

            AddCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dlg = new OpenFileDialog { AllowMultiple = true };
                dlg.Filters.Add(new FileDialogFilter() { Name = "Supported Files (*.svg;*.svgz)", Extensions = new List<string> { "svg", "svgz" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "SVG Files (*.svg)", Extensions = new List<string> { "svg" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "SVGZ Files (*.svgz)", Extensions = new List<string> { "svgz" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
                var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
                if (result is { })
                {
                    var paths = result.ToList();
                    foreach (var path in paths)
                    {
                        await Add(path);
                    }
                }
            });

            CopySelectedCommand = ReactiveCommand.CreateFromTask<string>(async format =>
            {
                if (_selectedItem is null || string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                var xaml = await ToXaml(_selectedItem, _enableGenerateImage);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        Application.Current.Clipboard.SetTextAsync(SvgConverter.Format(xaml));
                    }
                    catch
                    {
                        // ignored
                    }
                });
            });

            CopyAllCommand = ReactiveCommand.CreateFromTask<string>(async format =>
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                var paths = Items?.Select(x => x.Path).ToList();
                if (paths is { })
                {
                    var xaml = SvgConverter.ToXamlStyles(paths, generateImage: _enableGenerateImage, generatePreview: _enableGeneratePreview);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            Application.Current.Clipboard.SetTextAsync(SvgConverter.Format(xaml));
                        }
                        catch
                        {
                            // ignored
                        }
                    });
                }
            });

            ExportSelectedCommand = ReactiveCommand.CreateFromTask<string>(async format =>
            {
                if (_selectedItem is null || string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                var dlg = new SaveFileDialog();
                dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> { "axaml" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> { "xaml" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
                dlg.InitialFileName = Path.GetFileNameWithoutExtension(_selectedItem.Path);
                var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
                if (result is { })
                {
                    var xaml = await ToXaml(_selectedItem, _enableGenerateImage);

                    try
                    {
                        await Task.Run(() => File.WriteAllText(result, SvgConverter.Format(xaml)));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            });

            ExportAllCommand = ReactiveCommand.CreateFromTask<string>(async format =>
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                var dlg = new SaveFileDialog();
                dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> { "axaml" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> { "xaml" } });
                dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
                dlg.InitialFileName = Path.GetFileNameWithoutExtension("Svg");
                var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
                if (result is { })
                {
                    var paths = Items?.Select(x => x.Path).ToList();
                    if (paths is { })
                    {
                        var xaml = SvgConverter.ToXamlStyles(paths, generateImage: _enableGenerateImage, generatePreview: _enableGeneratePreview);

                        try
                        {
                            await Task.Run(() => File.WriteAllText(result, SvgConverter.Format(xaml)));
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            });

            ClipboardCommand = ReactiveCommand.CreateFromTask<string>(async format =>
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    return;
                }

                var text = await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        return await Application.Current.Clipboard.GetTextAsync();
                    }
                    catch
                    {
                        // ignored
                    }

                    return "";
                });

                var svg = new SKSvg();

                try
                {

                    svg.FromSvg(text);
                }
                catch
                {
                    // ignored
                }

                var xaml = await Task.Run(() =>
                {
                    if (_enableGenerateImage)
                    {
                        
                        return SvgConverter.ToXamlImage(svg.Model, key: null);
                    }
                    else
                    {
                        return SvgConverter.ToXamlDrawingGroup(svg.Model, key: null);
                    }
                });

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        Application.Current.Clipboard.SetTextAsync(SvgConverter.Format(xaml));
                    }
                    catch
                    {
                        // ignored
                    }
                });
            });

            this.WhenAnyValue(x => x.SelectedItem).Subscribe(async x =>
            {
                if (x is { })
                {
                    await x.Load();
                }
            });
        }

        private static async Task<string> ToXaml(FileItemViewModel fileItemViewModel, bool enableGenerateImage)
        {
            return await Task.Run(async () =>
            {
                if (fileItemViewModel.Picture is null)
                {
                    await fileItemViewModel.Load();
                }

                if (fileItemViewModel.Svg is { })
                {
                    if (enableGenerateImage)
                    {
                        return SvgConverter.ToXamlImage(fileItemViewModel.Svg.Model, key: null);
                    }
                    else
                    {
                        return SvgConverter.ToXamlDrawingGroup(fileItemViewModel.Svg.Model, key: null);
                    }
                }

                return "";
            });
        }

        public async void Drop(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                {
                    var svgPaths = Directory.EnumerateFiles(path, "*.svg", SearchOption.AllDirectories);
                    var svgzPaths = Directory.EnumerateFiles(path, "*.svgz", SearchOption.AllDirectories);
                    Drop(svgPaths);
                    Drop(svgzPaths);
                    continue;
                }

                var extension = Path.GetExtension(path);
                switch (extension.ToLower())
                {
                    case ".svg":
                    case ".svgz":
                        await Add(path);
                        break;
                }
            }
        }

        private async Task Add(string path)
        {
            if (_items is { })
            {
                var item = await Task.Run(() => new FileItemViewModel(Path.GetFileName(path), path, Preview, Remove));
                _items.Add(item);
            }
        }

        private async Task Preview(FileItemViewModel item)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (item.Svg is null)
                {
                    await item.Load();
                }

                if (item.Svg is null)
                {
                    return;
                }

                var xaml = SvgConverter.Format(SvgConverter.ToXamlDrawingGroup(item.Svg.Model, key: null));

                var sb = new StringBuilder();

                sb.Append($"<Viewbox xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
                sb.Append($"<Image>");
                sb.Append($"<DrawingImage>");
                sb.Append($"{xaml}");
                sb.Append($"</DrawingImage>");
                sb.Append($"</Image>");
                sb.Append($"</Viewbox>");

                var viewboxXaml = sb.ToString();
 
                var viewbox = AvaloniaRuntimeXamlLoader.Parse<Viewbox>(viewboxXaml);
                
                var window = new PreviewWindow()
                {
                    Content = viewbox,
                    Width = 800,
                    Height = 600
                };
  
               await window.ShowDialog((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            });
        }

        private async Task Remove(FileItemViewModel item)
        {
            await Task.Run(() =>
            {
                if (_items is { })
                {
                    _items.Remove(item);
                }
            });
        }
    }
}
