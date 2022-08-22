using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ReactiveMarbles.PropertyChanged;
using SvgToXaml.Views;
using ResourceDictionary = SvgToXamlConverter.Model.Resources.ResourceDictionary;

namespace SvgToXaml.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ProjectViewModel _project;

    [JsonInclude]
    public ProjectViewModel Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    [JsonIgnore]
    public ICommand ClearCommand { get; }
        
    [JsonIgnore]
    public ICommand OpenCommand { get; }

    [JsonIgnore]
    public ICommand SaveCommand { get; }

    [JsonIgnore]
    public ICommand AddCommand { get; }

    [JsonIgnore]
    public ICommand CopySelectedCommand { get; }

    [JsonIgnore]
    public ICommand CopyAllCommand { get; }

    [JsonIgnore]
    public ICommand ExportSelectedCommand { get; }

    [JsonIgnore]
    public ICommand ExportAllCommand { get; }

    [JsonIgnore]
    public ICommand ClipboardCommand { get; }

    [JsonConstructor]
    public MainWindowViewModel()
    {
        _project = new ProjectViewModel();
            
        ClearCommand = new RelayCommand(Clear);

        OpenCommand = new AsyncRelayCommand(async () => await Open());

        SaveCommand = new AsyncRelayCommand(async () => await Save());

        AddCommand = new AsyncRelayCommand(async () => await Add());

        CopySelectedCommand = new AsyncRelayCommand<string>(async format => await CopySelected(format));

        CopyAllCommand = new AsyncRelayCommand<string>(async format => await CopyAll(format));

        ExportSelectedCommand = new AsyncRelayCommand<string>(async format => await ExportSelected(format));

        ExportAllCommand = new AsyncRelayCommand<string>(async format => await ExportAll(format));

        ClipboardCommand = new AsyncRelayCommand<string>(async format => await Clipboard(format));

        // ReSharper disable once AsyncVoidLambda
        this.WhenChanged(x => x.Project.SelectedItem).DistinctUntilChanged().Subscribe(async x =>
        {
            if (x is { })
            {
                await x.Load(Project.GetIgnoreAttributes());
            }
        });
  
        // ReSharper disable once AsyncVoidLambda
        this.WhenChanged(x => x.Project.Settings.UseCompatMode).DistinctUntilChanged().Subscribe(async _ =>
        {
            await Reload();
        });

        // ReSharper disable once AsyncVoidLambda
        this.WhenChanged(x => x.Project.Settings.IgnoreOpacity).DistinctUntilChanged().Subscribe(async _ =>
        {
            await Reload();
        });

        // ReSharper disable once AsyncVoidLambda
        this.WhenChanged(x => x.Project.Settings.IgnoreFilter).DistinctUntilChanged().Subscribe(async _ =>
        {
            await Reload();
        });

        // ReSharper disable once AsyncVoidLambda
        this.WhenChanged(x => x.Project.Settings.IgnoreClipPath).DistinctUntilChanged().Subscribe(async _ =>
        {
            await Reload();
        });

        // ReSharper disable once AsyncVoidLambda
        this.WhenChanged(x => x.Project.Settings.IgnoreMask).DistinctUntilChanged().Subscribe(async _ =>
        {
            await Reload();
        });
    }

    private void Clear()
    {
        Project.SelectedItem = null;
        Project.Items.Clear();
    }

    private List<FilePickerFileType> GetOpenFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.Json,
            StorageService.All
        };
    }

    private static List<FilePickerFileType> GetSaveFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.Json,
            StorageService.All
        };
    }

    private static List<FilePickerFileType> GetImportFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.ImageSvg,
            StorageService.All
        };
    }

    private static List<FilePickerFileType> GetExportFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.ImageSvg,
            StorageService.All
        };
    }

    private async Task Open()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open project",
            FileTypeFilter = GetOpenFileTypes(),
            AllowMultiple = false
        });

        var file = result.FirstOrDefault();

        if (file is not null && file.CanOpenRead)
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                var project = JsonSerializer.Deserialize<ProjectViewModel>(json);
                if (project is { })
                {
                    Project = project;

                    await Task.Run(() =>
                    {
                        foreach (var fileItemViewModel in Project.Items)
                        {
                            Initialize(fileItemViewModel);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    private async Task Save()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save project",
            FileTypeChoices = GetSaveFileTypes(),
            SuggestedFileName = Path.GetFileNameWithoutExtension("project"),
            DefaultExtension = "json",
            ShowOverwritePrompt = true
        });

        if (file is not null && file.CanOpenWrite)
        {
            try
            {
                var json = await Task.Run(() => JsonSerializer.Serialize(Project));
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    private async Task Add()
    {
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null)
        {
            return;
        }
        var dlg = new OpenFileDialog { AllowMultiple = true };
        dlg.Filters.Add(new FileDialogFilter() { Name = "Supported Files (*.svg;*.svgz)", Extensions = new List<string> { "svg", "svgz" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "SVG Files (*.svg)", Extensions = new List<string> { "svg" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "SVGZ Files (*.svgz)", Extensions = new List<string> { "svgz" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
        var result = await dlg.ShowAsync(window);
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
        if (Project.SelectedItem is null || string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var xaml = await ToXaml(Project.SelectedItem, Project.Settings.EnableGenerateImage);

        await SetClipboard(xaml);
    }

    private async Task CopyAll(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var paths = Project.Items.Select(x => x.Path).ToList();
        var xaml = await ToXamlStyles(paths);
        await SetClipboard(xaml);
    }

    private async Task ExportSelected(string format)
    {
        if (Project.SelectedItem is null || string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null)
        {
            return;
        }

        var dlg = new SaveFileDialog();
        dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> { "axaml" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> { "xaml" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
        dlg.InitialFileName = Path.GetFileNameWithoutExtension(Project.SelectedItem.Path);
        var result = await dlg.ShowAsync(window);
        if (result is { })
        {
            var xaml = await ToXaml(Project.SelectedItem, Project.Settings.EnableGenerateImage);

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

        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (window is null)
        {
            return;
        }

        var dlg = new SaveFileDialog();
        dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> { "axaml" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> { "xaml" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
        dlg.InitialFileName = Path.GetFileNameWithoutExtension("Svg");
        var result = await dlg.ShowAsync(window);
        if (result is { })
        {
            var paths = Project.Items.Select(x => x.Path).ToList();
            if (paths.Count > 0)
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
                if (Application.Current?.Clipboard is { } clipboard)
                {
                    return await clipboard.GetTextAsync();
                }
            }
            catch
            {
                // ignored
            }

            return "";
        });

        var skSvg = new SvgViewModel();

        try
        {
            skSvg.FromSvg(svg, Project.GetIgnoreAttributes());
        }
        catch
        {
            // ignored
        }

        var text = await Task.Run(() =>
        {
            if (Project.Settings.EnableGenerateImage)
            {
                var converter = new SvgToXamlConverter.SvgToXamlConverter()
                {
                    UseCompatMode = Project.Settings.UseCompatMode,
                    ReuseExistingResources = Project.Settings.ReuseExistingResources,
                    Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
                };

                var xaml = converter.ToXamlImage(skSvg.Model);
                return converter.Format(xaml);
            }
            else
            {
                var converter = new SvgToXamlConverter.SvgToXamlConverter()
                {
                    UseCompatMode = Project.Settings.UseCompatMode,
                    ReuseExistingResources = Project.Settings.ReuseExistingResources,
                    Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
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
                if (Application.Current?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(xaml);
                }
            }
            catch
            {
                // ignored
            }
        });
    }

    private async Task Reload()
    {
        var items = Project.Items;
        if (items.Count == 0)
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

        if (Project.SelectedItem is { } selectedItem)
        {
            await selectedItem.Load(Project.GetIgnoreAttributes());
        }
    }
        
    private async Task<string> ToXaml(FileItemViewModel fileItemViewModel, bool enableGenerateImage)
    {
        return await Task.Run(async () =>
        {
            if (fileItemViewModel.Picture is null)
            {
                await fileItemViewModel.Load(Project.GetIgnoreAttributes());
            }

            if (fileItemViewModel.Svg is { })
            {
                if (enableGenerateImage)
                {
                    var converter = new SvgToXamlConverter.SvgToXamlConverter()
                    {
                        UseCompatMode = Project.Settings.UseCompatMode,
                        AddTransparentBackground = Project.Settings.AddTransparentBackground,
                        ReuseExistingResources = Project.Settings.ReuseExistingResources,
                        Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
                    };

                    var xaml = converter.ToXamlImage(fileItemViewModel.Svg.Model);
                    return converter.Format(xaml);
                }
                else
                {
                    var converter = new SvgToXamlConverter.SvgToXamlConverter()
                    {
                        UseCompatMode = Project.Settings.UseCompatMode,
                        AddTransparentBackground = Project.Settings.AddTransparentBackground,
                        ReuseExistingResources = Project.Settings.ReuseExistingResources,
                        Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
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
            var converter = new SvgToXamlConverter.SvgToXamlConverter()
            {
                UseCompatMode = Project.Settings.UseCompatMode, 
                ReuseExistingResources = Project.Settings.ReuseExistingResources,
                Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
            };

            var xaml = converter.ToXamlStyles(paths, Project.Settings.EnableGenerateImage, Project.Settings.EnableGeneratePreview);
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
        var item = await Task.Run(() => new FileItemViewModel(Path.GetFileName(path), path, Preview, Remove));
        Project.Items.Add(item);
    }

    private async Task Preview(FileItemViewModel item)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (item.Svg is null)
            {
                await item.Load(Project.GetIgnoreAttributes());
            }

            if (item.Svg is null)
            {
                return;
            }

            try
            {
                var converter = new SvgToXamlConverter.SvgToXamlConverter()
                {
                    UseCompatMode = Project.Settings.UseCompatMode,
                    ReuseExistingResources = Project.Settings.ReuseExistingResources,
                    Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
                };

                var image = converter.ToXamlImage(item.Svg.Model);

                image = converter.Format(image);

                var sb = new StringBuilder();

                sb.Append($"<TabControl xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");

                sb.Append($"<TabItem Header=\"Preview\">");
                sb.Append($"<Viewbox>");
                sb.Append($"{image}");
                sb.Append($"</Viewbox>");
                sb.Append($"</TabItem>");

                sb.Append($"<TabItem Header=\"Xaml\">");
                sb.Append($"<TextBox Text=\"{{Binding}}\" AcceptsReturn=\"True\" />");
                sb.Append($"</TabItem>");

                sb.Append($"</TabControl>");

                var c = sb.ToString();
     
                var content = AvaloniaRuntimeXamlLoader.Parse<TabControl>(c);
                    
                var window = new PreviewWindow()
                {
                    Content = content,
                    Width = 800,
                    Height = 600,
                    DataContext = image
                };

                var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                if (owner is { })
                {
                    await window.ShowDialog(owner);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        });
    }

    private async Task Remove(FileItemViewModel item)
    {
        await Task.Run(() =>
        {
            Project.Items.Remove(item);
        });
    }

    public void Initialize(FileItemViewModel item)
    {
        item.Initialize(Preview, Remove);
    }
        
    public void Add(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var item = new FileItemViewModel(Path.GetFileName(path), path, Preview, Remove);
            Project.Items.Add(item);
        }
    }
}
