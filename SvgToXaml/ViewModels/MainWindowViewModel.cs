using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using Svg.Skia;
using SvgToXamlConverter;

namespace SvgToXaml.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<FileItemViewModel>? _items;
        private FileItemViewModel? _selectedItem;
        private bool _enableGenerateImage;

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

        public ICommand ClearCommand { get; }
        
        public ICommand AddCommand { get; }

        public ICommand CopyCommand { get; }

        public ICommand ExportCommand { get; }

        public ICommand ClipboardCommand { get; }

        public MainWindowViewModel()
        {
            _items = new ObservableCollection<FileItemViewModel>();

            _enableGenerateImage = true;

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

            CopyCommand = ReactiveCommand.CreateFromTask<string>(async format =>
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
                        Application.Current.Clipboard.SetTextAsync(xaml);
                    }
                    catch
                    {
                        // ignored
                    }
                });
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

                var xaml = await Task.Run(() => SvgConverter.ToXaml(svg.Model, generateImage: _enableGenerateImage, indent: "", key: null));

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        Application.Current.Clipboard.SetTextAsync(xaml);
                    }
                    catch
                    {
                        // ignored
                    }
                });
            });

            ExportCommand = ReactiveCommand.CreateFromTask<string>(async format =>
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

                    await File.WriteAllTextAsync(result, xaml);
                }
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
                    return SvgConverter.ToXaml(fileItemViewModel.Svg.Model, generateImage: enableGenerateImage, indent: "", key: null);
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
                    var svgPaths = Directory.EnumerateFiles(path, "*.svg", new EnumerationOptions {RecurseSubdirectories = true});
                    var svgzPaths = Directory.EnumerateFiles(path, "*.svgz", new EnumerationOptions {RecurseSubdirectories = true});
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
                var item = await Task.Run(() => new FileItemViewModel(Path.GetFileName(path), path, x => _items.Remove(x)));
                _items.Add(item);
            }
        }
    }
}
