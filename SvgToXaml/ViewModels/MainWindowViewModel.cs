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
using Avalonia.Threading;
using ReactiveUI;
using Svg.Skia;
using SvgToXaml.Views;
using SvgToXamlConverter;

namespace SvgToXaml.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private FileItemViewModel? _selectedItem;
        private ObservableCollection<FileItemViewModel>? _items;
        private SettingsViewModel _settings;

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

        public SettingsViewModel Settings
        {
            get => _settings;
            set => this.RaiseAndSetIfChanged(ref _settings, value);
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

            _settings = new SettingsViewModel()
            {
                EnableGenerateImage = true,
                EnableGeneratePreview = true,
                UseResources = true,
                ReuseExistingResources = true,
                UseCompatMode = false,
                UseBrushTransform = false,
            };
        
            ClearCommand = ReactiveCommand.Create(Clear);

            AddCommand = ReactiveCommand.CreateFromTask(async () => await Add());

            CopySelectedCommand = ReactiveCommand.CreateFromTask<string>(async format => await CopySelected(format));

            CopyAllCommand = ReactiveCommand.CreateFromTask<string>(async format => await CopyAll(format));

            ExportSelectedCommand = ReactiveCommand.CreateFromTask<string>(async format => await ExportSelected(format));

            ExportAllCommand = ReactiveCommand.CreateFromTask<string>(async format => await ExportAll(format));

            ClipboardCommand = ReactiveCommand.CreateFromTask<string>(async format => await Clipboard(format));

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.SelectedItem).Subscribe(async x =>
            {
                if (x is { })
                {
                    await x.Load();
                }
            });
  
            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Settings.UseCompatMode).Subscribe(async _ =>
            {
                await Reload();
            });

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Settings.UseBrushTransform).Subscribe(async _ =>
            {
                await Reload();
            });
        }

        private void Clear()
        {
            SelectedItem = null;
            _items?.Clear();
        }

        private async Task Add()
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
        }

        private async Task CopySelected(string format)
        {
            if (_selectedItem is null || string.IsNullOrWhiteSpace(format))
            {
                return;
            }

            var xaml = await ToXaml(_selectedItem, _settings.EnableGenerateImage);

            await SetClipboard(xaml);
        }

        private async Task CopyAll(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return;
            }

            var paths = Items?.Select(x => x.Path).ToList();
            if (paths is { })
            {
                var xaml = await ToXamlStyles(paths);
                await SetClipboard(xaml);
            }
        }

        private async Task ExportSelected(string format)
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
                var xaml = await ToXaml(_selectedItem, _settings.EnableGenerateImage);

                try
                {
                    await Task.Run(() => File.WriteAllText(result, xaml));
                }
                catch
                {
                    // ignored
                }
            }
        }

        private async Task ExportAll(string format)
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
                    try
                    {
                        var xaml = await ToXamlStyles(paths);
                        await Task.Run(() => File.WriteAllText(result, xaml));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        private async Task Clipboard(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return;
            }

            var svg = await Dispatcher.UIThread.InvokeAsync(async () =>
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

            var skSvg = new SKSvg();

            try
            {
                skSvg.FromSvg(svg);
            }
            catch
            {
                // ignored
            }

            var text = await Task.Run(() =>
            {
                if (_settings.EnableGenerateImage)
                {
                    var converter = new SvgConverter()
                    {
                        UseCompatMode = _settings.UseCompatMode,
                        UseBrushTransform = _settings.UseBrushTransform,
                        ReuseExistingResources = _settings.ReuseExistingResources,
                        Resources = _settings.UseResources ? new SvgToXamlConverter.ResourceDictionary() : null
                    };

                    var xaml = converter.ToXamlImage(skSvg.Model, writeResources: true);
                    return converter.Format(xaml);
                }
                else
                {
                    var converter = new SvgConverter()
                    {
                        UseCompatMode = _settings.UseCompatMode,
                        UseBrushTransform = _settings.UseBrushTransform,
                        ReuseExistingResources = _settings.ReuseExistingResources,
                        Resources = _settings.UseResources ? new SvgToXamlConverter.ResourceDictionary() : null
                    };

                    var xaml = converter.ToXamlDrawingGroup(skSvg.Model);
                    return converter.Format(xaml);
                }
            });

            await SetClipboard(text);
        }

        private async Task SetClipboard(string? xaml)
        {
            if (xaml is not { })
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    await Application.Current.Clipboard.SetTextAsync(xaml);
                }
                catch
                {
                    // ignored
                }
            });
        }

        private async Task Reload()
        {
            var items = _items;

            if (items is null || items.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                foreach (var fileItemViewModel in items)
                {
                    fileItemViewModel.Clean();
                }
            });

            if (_selectedItem is { } selectedItem)
            {
                await selectedItem.Load();
            }
        }
        
        private async Task<string> ToXaml(FileItemViewModel fileItemViewModel, bool enableGenerateImage)
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
                        var converter = new SvgConverter()
                        {
                            UseCompatMode = _settings.UseCompatMode,
                            UseBrushTransform = _settings.UseBrushTransform,
                            ReuseExistingResources = _settings.ReuseExistingResources,
                            Resources = _settings.UseResources ? new SvgToXamlConverter.ResourceDictionary() : null
                        };

                        var xaml = converter.ToXamlImage(fileItemViewModel.Svg.Model, writeResources: true);
                        return converter.Format(xaml);
                    }
                    else
                    {
                        var converter = new SvgConverter()
                        {
                            UseCompatMode = _settings.UseCompatMode,
                            UseBrushTransform = _settings.UseBrushTransform,
                            ReuseExistingResources = _settings.ReuseExistingResources,
                            Resources = _settings.UseResources ? new SvgToXamlConverter.ResourceDictionary() : null
                        };

                        var xaml = converter.ToXamlDrawingGroup(fileItemViewModel.Svg.Model);
                        return converter.Format(xaml);
                    }
                }

                return "";
            });
        }

        private async Task<string> ToXamlStyles(List<string> paths)
        {
            return await Task.Run(() =>
            {
                var converter = new SvgConverter()
                {
                    UseCompatMode = _settings.UseCompatMode, 
                    UseBrushTransform = _settings.UseBrushTransform,
                    ReuseExistingResources = _settings.ReuseExistingResources,
                    Resources = _settings.UseResources ? new SvgToXamlConverter.ResourceDictionary() : null
                };

                var xaml = converter.ToXamlStyles(paths, _settings.EnableGenerateImage, _settings.EnableGeneratePreview);
                return converter.Format(xaml);
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

                try
                {
                    var converter = new SvgConverter()
                    {
                        UseCompatMode = _settings.UseCompatMode,
                        UseBrushTransform = _settings.UseBrushTransform,
                        ReuseExistingResources = _settings.ReuseExistingResources,
                        Resources = _settings.UseResources ? new SvgToXamlConverter.ResourceDictionary() : null
                    };

                    var xaml = converter.ToXamlDrawingGroup(item.Svg.Model);

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
                }
                catch
                {
                    // ignored
                }
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

        public void Add(IEnumerable<string> paths)
        {
            if (_items is { })
            {
                foreach (var path in paths)
                {
                    var item = new FileItemViewModel(Path.GetFileName(path), path, Preview, Remove);
                    _items.Add(item);
                }
            }
        }
    }
}
