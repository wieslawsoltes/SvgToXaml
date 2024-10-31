using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using ReactiveMarbles.PropertyChanged;
using SvgToXaml.Converter;
using SvgToXaml.Model;

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
            StorageService.ImageSvgz,
            StorageService.All
        };
    }

    private static List<FilePickerFileType> GetExportFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.Axaml,
            StorageService.Xaml,
            StorageService.All
        };
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window.Clipboard;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime { MainView: { } mainView })
        {
            if (mainView.GetVisualRoot() is TopLevel topLevel)
            {
                return topLevel.Clipboard;
            }
        }

        return null;
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

        if (file is not null)
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

        if (file is not null)
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

    private async Task<Stream> LoadFromStream(Stream stream, string name)
    {
        var extension = Path.GetExtension(name);
        var memoryStream = new MemoryStream();
        
        if (extension == "svgz")
        {
            await using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(memoryStream);
        }
        else
        {
            await stream.CopyToAsync(memoryStream);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }
    
    private async Task Add()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import svgs",
            FileTypeFilter = GetImportFileTypes(),
            AllowMultiple = true
        });

        foreach (var file in result)
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
                var ms = await LoadFromStream(stream, file.Name);
                await Add(ms, Path.GetFileName(file.Name));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    private async Task CopySelected(string? format)
    {
        if (Project.SelectedItem is null || string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var xaml = await ToXaml(Project.SelectedItem, Project.Settings.EnableGenerateImage);

        await SetClipboard(xaml);
    }

    private async Task CopyAll(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var inputItems = Project.Items.Select(x => new InputItem(x.Name, x.Content)).ToList();
        var xaml = await ToXamlStyles(inputItems);
        await SetClipboard(xaml);
    }

    private async Task ExportSelected(string? format)
    {
        if (Project.SelectedItem is null || string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save xaml",
            FileTypeChoices = GetExportFileTypes(),
            SuggestedFileName = Path.GetFileNameWithoutExtension(Project.SelectedItem.Name) + ".axaml",
            DefaultExtension = "axaml",
            ShowOverwritePrompt = true
        });

        if (file is not null)
        {
            try
            {
                var xaml = await ToXaml(Project.SelectedItem, Project.Settings.EnableGenerateImage);
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(xaml);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    private async Task ExportAll(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save xaml",
            FileTypeChoices = GetExportFileTypes(),
            SuggestedFileName = "Styles.axaml",
            DefaultExtension = "axaml",
            ShowOverwritePrompt = true
        });

        if (file is not null)
        {
            try
            {
                var paths = Project.Items.Select(x => new InputItem(x.Name, x.Content)).ToList();
                if (paths.Count > 0)
                {
                    var xaml = await ToXamlStyles(paths);
                    await using var stream = await file.OpenWriteAsync();
                    await using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(xaml);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }

    private async Task Clipboard(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return;
        }

        var svg = await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                if (GetClipboard() is {} clipboard)
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
                var converter = new SvgToXamlConverter()
                {
                    UseCompatMode = Project.Settings.UseCompatMode,
                    ReuseExistingResources = Project.Settings.ReuseExistingResources,
                    TransformGeometry = Project.Settings.TransformGeometry,
                    Resources = Project.Settings.UseResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
                };

                var xaml = converter.ToXamlImage(skSvg.Model);
                return converter.Format(xaml);
            }
            else
            {
                var converter = new SvgToXamlConverter()
                {
                    UseCompatMode = Project.Settings.UseCompatMode,
                    ReuseExistingResources = Project.Settings.ReuseExistingResources,
                    TransformGeometry = Project.Settings.TransformGeometry,
                    Resources = Project.Settings.UseResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
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
                if (GetClipboard() is {} clipboard)
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
                    var converter = new SvgToXamlConverter()
                    {
                        UseCompatMode = Project.Settings.UseCompatMode,
                        AddTransparentBackground = Project.Settings.AddTransparentBackground,
                        ReuseExistingResources = Project.Settings.ReuseExistingResources,
                        TransformGeometry = Project.Settings.TransformGeometry,
                        Resources = Project.Settings.UseResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
                    };

                    var xaml = converter.ToXamlImage(fileItemViewModel.Svg.Model);
                    return converter.Format(xaml);
                }
                else
                {
                    var converter = new SvgToXamlConverter()
                    {
                        UseCompatMode = Project.Settings.UseCompatMode,
                        AddTransparentBackground = Project.Settings.AddTransparentBackground,
                        ReuseExistingResources = Project.Settings.ReuseExistingResources,
                        TransformGeometry = Project.Settings.TransformGeometry,
                        Resources = Project.Settings.UseResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
                    };

                    var xaml = converter.ToXamlDrawingGroup(fileItemViewModel.Svg.Model);
                    return converter.Format(xaml);
                }
            }

            return "";
        });
    }

    private async Task<string> ToXamlStyles(List<InputItem> inputItems)
    {
        return await Task.Run(() =>
        {
            var converter = new SvgToXamlConverter()
            {
                UseCompatMode = Project.Settings.UseCompatMode, 
                ReuseExistingResources = Project.Settings.ReuseExistingResources,
                TransformGeometry = Project.Settings.TransformGeometry,
                Resources = Project.Settings.UseResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
            };

            var xaml = converter.ToXamlStyles(inputItems, Project.Settings.EnableGenerateImage, Project.Settings.EnableGeneratePreview);
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
                {
                    await using var stream = File.OpenRead(path);
                    var ms = await LoadFromStream(stream, path);
                    var name = Path.GetFileName(path);
                    await Add(ms, name);
                    break;
                }
            }
        }
    }

    private async Task Add(Stream stream, string name)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        var item = await Task.Run(() => new FileItemViewModel(name, content, Remove));
        Project.Items.Add(item);
    }

    public record PreviewItem(string Image, string TabControl);

    public async Task<PreviewItem?> GetPreview(FileItemViewModel item)
    {
        if (item.Svg is null)
        {
            await item.Load(Project.GetIgnoreAttributes());
        }

        if (item.Svg is null)
        {
            return null;
        }

        try
        {
            var converter = new SvgToXamlConverter()
            {
                UseCompatMode = Project.Settings.UseCompatMode,
                ReuseExistingResources = Project.Settings.ReuseExistingResources,
                TransformGeometry = Project.Settings.TransformGeometry,
                Resources = Project.Settings.UseResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
            };

            var image = converter.ToXamlImage(item.Svg.Model);

            image = converter.Format(image);

            var sb = new StringBuilder();

            sb.Append($"<TabControl xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");

            sb.Append($"<TabItem Header=\"Preview\">");
            sb.Append($"<Panel>");
            sb.Append($"<Border BorderThickness=\"0\" CornerRadius=\"0\" Background=\"DarkGray\" />");
            sb.Append($"<Border>");
            sb.Append($"<Border.Background>");
            sb.Append($"<VisualBrush TileMode=\"Tile\" SourceRect=\"0,0,20,20\" DestinationRect=\"0,0,20,20\">");
            sb.Append($"<VisualBrush.Visual>");
            sb.Append($"<Path Data=\"M 0,0 L 0,10 L 10,10 L 10,0 Z M 10,10 L 10,20 L 20,20 L 20,10 Z\" Fill=\"LightGray\" />");
            sb.Append($"</VisualBrush.Visual>");
            sb.Append($"</VisualBrush>");
            sb.Append($"</Border.Background>");
            sb.Append($"</Border>");
            sb.Append($"<Viewbox Margin=\"0\">");
            sb.Append($"{image}");
            sb.Append($"</Viewbox>");
            sb.Append($"</Panel>");
            sb.Append($"</TabItem>");

            sb.Append($"<TabItem Header=\"Xaml\">");
            sb.Append($"<TextBox Text=\"{{Binding}}\" IsReadOnly=\"True\" />");
            sb.Append($"</TabItem>");

            sb.Append($"</TabControl>");

            return new PreviewItem(image, sb.ToString());
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }

        return null;
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
        item.Initialize(Remove);
    }
}
