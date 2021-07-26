using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ReactiveUI;
using ShimSkiaSharp;
using Svg.Skia;
using SKPathArcSize = SkiaSharp.SKPathArcSize;
using SKPathDirection = SkiaSharp.SKPathDirection;

namespace SvgToXaml.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<FileItemViewModel>? _items;
        private FileItemViewModel? _selectedItem;

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

        public ICommand ClearCommand { get; }
        
        public ICommand AddCommand { get; }

        public ICommand CopyCommand { get; }

        public ICommand ExportCommand { get; }

        public ICommand ClipboardCommand { get; }
        
        public MainWindowViewModel()
        {
            _items = new ObservableCollection<FileItemViewModel>();

            ClearCommand = ReactiveCommand.Create(() =>
            {
                SelectedItem = null;
                _items?.Clear();
            });

            AddCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dlg = new OpenFileDialog {AllowMultiple = true};
                dlg.Filters.Add(new FileDialogFilter() { Name = "Supported Files (*.svg;*.svgz)", Extensions = new List<string> {"svg", "svgz"} });
                dlg.Filters.Add(new FileDialogFilter() { Name = "SVG Files (*.svg)", Extensions = new List<string> {"svg"} });
                dlg.Filters.Add(new FileDialogFilter() { Name = "SVGZ Files (*.svgz)", Extensions = new List<string> {"svgz"} });
                dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> {"*"} });
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

                var xaml = await ToXaml(_selectedItem);

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

                var xaml = await Task.Run(() => ToXaml(svg.Model));

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
                dlg.Filters.Add(new FileDialogFilter() { Name = "AXAML Files (*.axaml)", Extensions = new List<string> {"axaml"} });
                dlg.Filters.Add(new FileDialogFilter() { Name = "XAML Files (*.xaml)", Extensions = new List<string> {"xaml"} });
                dlg.Filters.Add(new FileDialogFilter() { Name = "All Files (*.*)", Extensions = new List<string> {"*"} });
                dlg.InitialFileName = Path.GetFileNameWithoutExtension(_selectedItem.Path);
                var result = await dlg.ShowAsync((Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
                if (result is { })
                {
                    var xaml = await ToXaml(_selectedItem);

                    await File.WriteAllTextAsync(result, xaml);
                }
            });
        }

       private static void ToPathData(SKPath path, StringBuilder sb)
       {
            if (path.FillType != SKPathFillType.Winding)
            {
                sb.Append($"F1 ");
            }

            if (path.Commands is null)
            {
                return;
            }

            for (var index = 0; index < path.Commands.Count; index++)
            {
                var pathCommand = path.Commands[index];

                switch (pathCommand)
                {
                    case MoveToPathCommand moveToPathCommand:
                    {
                        var x = moveToPathCommand.X;
                        var y = moveToPathCommand.Y;
                        sb.Append($"{(index > 0 ? " " : "")}M{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
                    }
                        break;
                    case LineToPathCommand lineToPathCommand:
                    {
                        var x = lineToPathCommand.X;
                        var y = lineToPathCommand.Y;
                        sb.Append($"{(index > 0 ? " " : "")}L{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
                    }
                        break;
                    case ArcToPathCommand arcToPathCommand:
                    {
                        var rx = arcToPathCommand.Rx;
                        var ry = arcToPathCommand.Ry;
                        var xAxisRotate = arcToPathCommand.XAxisRotate;
                        var largeArc = arcToPathCommand.LargeArc.ToSKPathArcSize();
                        var sweep = arcToPathCommand.Sweep.ToSKPathDirection();
                        var x = arcToPathCommand.X;
                        var y = arcToPathCommand.Y;
                        sb.Append($"{(index > 0 ? " " : "")}A{rx.ToString(CultureInfo.InvariantCulture)},{ry.ToString(CultureInfo.InvariantCulture)} {xAxisRotate.ToString(CultureInfo.InvariantCulture)}, {(largeArc == SKPathArcSize.Large ? "1" : "0")}, {(sweep == SKPathDirection.Clockwise ? "1" : "0")}, {x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)});");
                    }
                        break;
                    case QuadToPathCommand quadToPathCommand:
                    {
                        var x0 = quadToPathCommand.X0;
                        var y0 = quadToPathCommand.Y0;
                        var x1 = quadToPathCommand.X1;
                        var y1 = quadToPathCommand.Y1;
                        sb.Append($"{(index > 0 ? " " : "")}Q{x0.ToString(CultureInfo.InvariantCulture)},{y0.ToString(CultureInfo.InvariantCulture)} {x1.ToString(CultureInfo.InvariantCulture)},{y1.ToString(CultureInfo.InvariantCulture)}");
                    }
                        break;
                    case CubicToPathCommand cubicToPathCommand:
                    {
                        var x0 = cubicToPathCommand.X0;
                        var y0 = cubicToPathCommand.Y0;
                        var x1 = cubicToPathCommand.X1;
                        var y1 = cubicToPathCommand.Y1;
                        var x2 = cubicToPathCommand.X2;
                        var y2 = cubicToPathCommand.Y2;
                        sb.Append($"{(index > 0 ? " " : "")}C{x0.ToString(CultureInfo.InvariantCulture)},{y0.ToString(CultureInfo.InvariantCulture)} {x1.ToString(CultureInfo.InvariantCulture)},{y1.ToString(CultureInfo.InvariantCulture)} {x2.ToString(CultureInfo.InvariantCulture)},{y2.ToString(CultureInfo.InvariantCulture)}");
                    }
                        break;
                    case ClosePathCommand _:
                    {
                        sb.Append($" Z");
                    }
                        break;
                    case AddRectPathCommand addRectPathCommand:
                    {
                        // TODO:
                    }
                        break;
                    case AddRoundRectPathCommand addRoundRectPathCommand:
                    {
                        // TODO:
                    }
                        break;
                    case AddOvalPathCommand addOvalPathCommand:
                    {
                        // TODO:
                    }
                        break;
                    case AddCirclePathCommand addCirclePathCommand:
                    {
                        // TODO:
                    }
                        break;
                    case AddPolyPathCommand addPolyPathCommand:
                    {
                        if (addPolyPathCommand.Points is not null && addPolyPathCommand.Points.Count >= 2)
                        {
                            var mx = addPolyPathCommand.Points[0].X;
                            var my = addPolyPathCommand.Points[0].Y;
                            
                            sb.Append($"{(index > 0 ? " " : "")}M{mx.ToString(CultureInfo.InvariantCulture)},{my.ToString(CultureInfo.InvariantCulture)}");

                            for (int i = 1; i < addPolyPathCommand.Points.Count; i++)
                            {
                                var lx = addPolyPathCommand.Points[i].X;
                                var ly = addPolyPathCommand.Points[i].Y;
                                sb.Append($"{(index > 0 ? " " : "")}L{lx.ToString(CultureInfo.InvariantCulture)},{ly.ToString(CultureInfo.InvariantCulture)}");
                            }

                            if (addPolyPathCommand.Close)
                            {
                                sb.Append($" Z");
                            }
                        }
                    }
                        break;
                    default:
                        break;
                }
            }
        }

        private static string ToXaml(SKPicture? model)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup>\r\n");

            if (model?.Commands is { })
            {
                foreach (var canvasCommand in model.Commands)
                {
                    switch (canvasCommand)
                    {
                        case ClipPathCanvasCommand clipPathCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        case ClipRectCanvasCommand clipRectCanvasCommand:
                        {
                            // TODO:;
                        }
                            break;
                        case SaveCanvasCommand _:
                        {
                            // TODO:
                        }
                            break;
                        case RestoreCanvasCommand _:
                        {
                            // TODO:
                        }
                            break;
                        case SetMatrixCanvasCommand setMatrixCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        case SaveLayerCanvasCommand saveLayerCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        case DrawImageCanvasCommand drawImageCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        case DrawPathCanvasCommand drawPathCanvasCommand:
                        {
                            if (drawPathCanvasCommand.Path is { } && drawPathCanvasCommand.Paint is { })
                            {
                                var sbPath = new StringBuilder();

                                ToPathData(drawPathCanvasCommand.Path, sbPath);

                                var data = sbPath.ToString();

                                if (drawPathCanvasCommand.Paint.Style == SKPaintStyle.Fill)
                                {
                                    if (drawPathCanvasCommand.Paint.Shader is ColorShader colorShader)
                                    {
                                        var brush =
                                            $"#{colorShader.Color.Alpha:X2}{colorShader.Color.Red:X2}{colorShader.Color.Green:X2}{colorShader.Color.Blue:X2}";
                                        sb.Append($"  <GeometryDrawing Brush=\"{brush}\" Geometry=\"{data}\"/>\r\n");
                                    }
                                }
                            }
                        }
                            break;
                        case DrawTextBlobCanvasCommand drawPositionedTextCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        case DrawTextCanvasCommand drawTextCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        case DrawTextOnPathCanvasCommand drawTextOnPathCanvasCommand:
                        {
                            // TODO:
                        }
                            break;
                        default:
                            break;
                    }
                }
            }

            sb.Append($"</DrawingGroup>");

            return sb.ToString();
        }

        private static async Task<string> ToXaml(FileItemViewModel fileItemViewModel)
        {
            return await Task.Run(() => ToXaml(fileItemViewModel.Svg?.Model));
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
