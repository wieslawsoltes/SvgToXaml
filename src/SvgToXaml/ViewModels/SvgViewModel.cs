using Svg.Model;
using ShimSkiaSharp;
using Svg.Skia;

namespace SvgToXaml.ViewModels;

public class SvgViewModel : ViewModelBase
{
    private static readonly IAssetLoader s_assetLoader = new SkiaAssetLoader();

    public SKDrawable? Drawable { get; private set; }

    public SKPicture? Model { get; private set; }

    public SkiaSharp.SKPicture? Picture { get; private set; }

    private void Reset()
    {
        Model = null;
        Drawable = null;

        Picture?.Dispose();
        Picture = null;
    }

    public void Dispose()
    {
        Reset();
    }
        
    public SkiaSharp.SKPicture? Load(string path, DrawAttributes ignoreAttributes)
    {
        Reset();
        var svgDocument = SvgExtensions.Open(path);
        if (svgDocument is { })
        {
            Model = SvgExtensions.ToModel(svgDocument, s_assetLoader, out var drawable, out _, ignoreAttributes);
            Drawable = drawable;
            Picture = Model?.ToSKPicture();
            return Picture;
        }
        return null;
    }

    public SkiaSharp.SKPicture? FromSvg(string svg, DrawAttributes ignoreAttributes)
    {
        Reset();
        var svgDocument = SvgExtensions.FromSvg(svg);
        if (svgDocument is { })
        {
            Model = SvgExtensions.ToModel(svgDocument, s_assetLoader, out var drawable, out _, ignoreAttributes);
            Drawable = drawable;
            Picture = Model?.ToSKPicture();
            return Picture;
        }
        return null;
    }
}