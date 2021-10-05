using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReactiveUI;
using SvgToXaml.Views;
using SvgToXamlConverter;
using ResourceDictionary = SvgToXamlConverter.Model.Resources.ResourceDictionary;

namespace SvgToXaml.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ProjectViewModel _project;

        [JsonInclude]
        public ProjectViewModel Project
        {
            get => _project;
            set => this.RaiseAndSetIfChanged(ref _project, value);
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
            
            ClearCommand = ReactiveCommand.Create(Clear);

            OpenCommand = ReactiveCommand.CreateFromTask(async () => await Open());

            SaveCommand = ReactiveCommand.CreateFromTask(async () => await Save());

            AddCommand = ReactiveCommand.CreateFromTask(async () => await Add());

            CopySelectedCommand = ReactiveCommand.CreateFromTask<string>(async format => await CopySelected(format));

            CopyAllCommand = ReactiveCommand.CreateFromTask<string>(async format => await CopyAll(format));

            ExportSelectedCommand = ReactiveCommand.CreateFromTask<string>(async format => await ExportSelected(format));

            ExportAllCommand = ReactiveCommand.CreateFromTask<string>(async format => await ExportAll(format));

            ClipboardCommand = ReactiveCommand.CreateFromTask<string>(async format => await Clipboard(format));

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.SelectedItem).Subscribe(async x =>
            {
                if (x is { })
                {
                    await x.Load(Project.GetIgnoreAttributes());
                }
            });
  
            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.Settings.UseCompatMode).Subscribe(async _ =>
            {
                await Reload();
            });

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.Settings.UseBrushTransform).Subscribe(async _ =>
            {
                await Reload();
            });

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.Settings.IgnoreOpacity).Subscribe(async _ =>
            {
                await Reload();
            });

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.Settings.IgnoreFilter).Subscribe(async _ =>
            {
                await Reload();
            });

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.Settings.IgnoreClipPath).Subscribe(async _ =>
            {
                await Reload();
            });

            // ReSharper disable once AsyncVoidLambda
            this.WhenAnyValue(x => x.Project.Settings.IgnoreMask).Subscribe(async _ =>
            {
                await Reload();
            });
        }

        private void Clear()
        {
            Project.SelectedItem = null;
            Project.Items.Clear();
        }

        private async Task Open()
        {
            var dlg = new OpenFileDialog { AllowMultiple = false };
            dlg.Filters.Add(new FileDialogFilter() { Name = "Project Files (*.json)", Extensions = new List<string> { "json" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
            var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            if (result is { } && result.Length == 1)
            {
                var json = await Task.Run(() => File.ReadAllText(result.First()));
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
        }
        
        private async Task Save()
        {
            var dlg = new SaveFileDialog();
            dlg.Filters.Add(new FileDialogFilter() { Name = "Project Files (*.json)", Extensions = new List<string> { "json" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
            dlg.InitialFileName = Path.GetFileNameWithoutExtension("project");
            var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            if (result is { })
            {
                try
                {
                    await Task.Run(() =>
                    {
                        var json = JsonSerializer.Serialize(Project);
                        File.WriteAllText(result, json);
                    });
                }
                catch
                {
                    // ignored
                }
            }
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

            var dlg = new SaveFileDialog();
            dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> { "axaml" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> { "xaml" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
            dlg.InitialFileName = Path.GetFileNameWithoutExtension(Project.SelectedItem.Path);
            var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
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

            var dlg = new SaveFileDialog();
            dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> { "axaml" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> { "xaml" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> { "*" } });
            dlg.InitialFileName = Path.GetFileNameWithoutExtension("Svg");
            var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
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
                    return await Application.Current.Clipboard.GetTextAsync();
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
                        UseBrushTransform = Project.Settings.UseBrushTransform,
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
                        UseBrushTransform = Project.Settings.UseBrushTransform,
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
                            UseBrushTransform = Project.Settings.UseBrushTransform,
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
                            UseBrushTransform = Project.Settings.UseBrushTransform,
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
                    UseBrushTransform = Project.Settings.UseBrushTransform,
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
                        UseBrushTransform = Project.Settings.UseBrushTransform,
                        ReuseExistingResources = Project.Settings.ReuseExistingResources,
                        Resources = Project.Settings.UseResources ? new ResourceDictionary() : null
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
}
