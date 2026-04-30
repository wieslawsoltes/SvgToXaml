using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using Svg.Model;

namespace SvgToXaml.ViewModels;

public class FileItemViewModel : ViewModelBase
{
    private const int MaxPreviewDimension = 1024;
    private const double MaxPreviewPixels = MaxPreviewDimension * MaxPreviewDimension;

    private bool _isLoading;
    private string _name;
    private string _content;
    private SvgViewModel? _svg;
    private SkiaSharp.SKPicture? _picture;
    private Bitmap? _previewBitmap;

    [JsonInclude]
    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    [JsonInclude]
    public string Content
    {
        get => _content;
        private set => SetProperty(ref _content, value);
    }

    [JsonIgnore]
    public SvgViewModel? Svg
    {
        get => _svg;
        private set => SetProperty(ref _svg, value);
    }

    [JsonIgnore]
    public SkiaSharp.SKPicture? Picture
    {
        get => _picture;
        private set => SetProperty(ref _picture, value);
    }

    [JsonIgnore]
    public Bitmap? PreviewBitmap
    {
        get => _previewBitmap;
        private set => SetProperty(ref _previewBitmap, value);
    }

    [JsonIgnore]
    public ICommand? RemoveCommand { get; private set; }

    [JsonConstructor]
    public FileItemViewModel(string name, string content)
    {
        _name = name;
        _content = content;
    }
        
    public FileItemViewModel(string name, string content, Func<FileItemViewModel, Task> remove) 
        : this(name, content)
    {
        Initialize(remove);
    }

    public void Initialize(Func<FileItemViewModel, Task> remove)
    {
        RemoveCommand = new AsyncRelayCommand(async () => await remove(this)); 
    }
 
    public async Task Load(DrawAttributes ignoreAttribute)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        try
        {
            if (Picture is null)
            {
                await Task.Run(() =>
                {
                    var previousSvg = Svg;
                    var previousPicture = Picture;
                    var previousPreviewBitmap = PreviewBitmap;

                    SvgViewModel? svg = null;
                    SkiaSharp.SKPicture? picture = null;
                    Bitmap? previewBitmap = null;

                    try
                    {
                        svg = new SvgViewModel();
                        picture = svg.FromSvg(Content, ignoreAttribute);
                        previewBitmap = CreatePreviewBitmap(picture);

                        Svg = svg;
                        Picture = picture;
                        PreviewBitmap = previewBitmap;

                        svg = null;
                        picture = null;
                        previewBitmap = null;
                    }
                    finally
                    {
                        previousPreviewBitmap?.Dispose();
                        previousPicture?.Dispose();
                        previousSvg?.Dispose();

                        previewBitmap?.Dispose();
                        picture?.Dispose();
                        svg?.Dispose();
                    }
                });
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void Clean()
    {
        PreviewBitmap?.Dispose();
        PreviewBitmap = null;

        Picture?.Dispose();
        Picture = null;

        Svg?.Dispose();
        Svg = null;
    }

    private static Bitmap? CreatePreviewBitmap(SKPicture? picture)
    {
        if (picture is null)
        {
            return null;
        }

        var bounds = picture.CullRect;
        var sourceWidth = Math.Max((int)Math.Ceiling(bounds.Width), 1);
        var sourceHeight = Math.Max((int)Math.Ceiling(bounds.Height), 1);
        var scale = CalculatePreviewScale(sourceWidth, sourceHeight);
        var width = Math.Max((int)Math.Ceiling(sourceWidth * scale), 1);
        var height = Math.Max((int)Math.Ceiling(sourceHeight * scale), 1);

        using var bitmap = new SKBitmap(width, height, isOpaque: false);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)scale);
        canvas.Translate(-bounds.Left, -bounds.Top);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        if (data is null)
        {
            return null;
        }

        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }

    private static double CalculatePreviewScale(int width, int height)
    {
        var scaleByDimension = Math.Min(
            1d,
            Math.Min((double)MaxPreviewDimension / width, (double)MaxPreviewDimension / height));

        var scaleByPixels = Math.Min(1d, Math.Sqrt(MaxPreviewPixels / (width * (double)height)));

        return Math.Min(scaleByDimension, scaleByPixels);
    }
}
