using System.IO;
using Svg.Model;
using Svg.Model.Services;
using ShimSkiaSharp;
using Svg.Skia;

namespace SvgToXaml.ViewModels;

public class SvgViewModel : ViewModelBase
{
    private static readonly SkiaModel s_model;
    private static readonly ISvgAssetLoader s_assetLoader;

    public SKDrawable? Drawable { get; private set; }

    public SKPicture? Model { get; private set; }

    public SkiaSharp.SKPicture? Picture { get; private set; }

    static SvgViewModel()
    {
        s_model = new SkiaModel(new SKSvgSettings());
        s_assetLoader = new SkiaSvgAssetLoader(s_model);
    }

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
        
    public SkiaSharp.SKPicture? Load(Stream stream, DrawAttributes ignoreAttributes)
    {
        Reset();
        var svgDocument = SvgService.Open(stream);
        if (svgDocument is { })
        {
            Model = SvgService.ToModel(svgDocument, s_assetLoader, out var drawable, out _, ignoreAttributes);
            Drawable = drawable;
            Picture = s_model.ToSKPicture(Model);
            return Picture;
        }
        return null;
    }

    public SkiaSharp.SKPicture? FromSvg(string svg, DrawAttributes ignoreAttributes)
    {
        Reset();
        var svgDocument = SvgService.FromSvg(svg);
        if (svgDocument is { })
        {
            Model = SvgService.ToModel(svgDocument, s_assetLoader, out var drawable, out _, ignoreAttributes);
            Drawable = drawable;
            Picture = s_model.ToSKPicture(Model);
            return Picture;
        }
        return null;
    }
}
