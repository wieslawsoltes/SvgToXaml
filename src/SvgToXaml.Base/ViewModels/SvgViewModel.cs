using System;
using System.IO;
using Svg.Model;
using ShimSkiaSharp;
using Svg.Skia;

namespace SvgToXaml.ViewModels;

public class SvgViewModel : ViewModelBase, IDisposable
{
    private SKSvg? _svg;

    public SKPicture? Model { get; private set; }

    public SkiaSharp.SKPicture? Picture { get; private set; }

    private void Reset()
    {
        _svg?.Dispose();
        _svg = null;

        Model = null;
        Picture = null;
    }

    public void Dispose()
    {
        Reset();
    }
        
    public SkiaSharp.SKPicture? Load(Stream stream, DrawAttributes ignoreAttributes)
    {
        Reset();
        _svg = new SKSvg
        {
            IgnoreAttributes = ignoreAttributes
        };

        Picture = _svg.Load(stream);
        Model = _svg.Model;
        if (Picture is { } && Model is { })
        {
            return Picture;
        }

        return null;
    }

    public SkiaSharp.SKPicture? FromSvg(string svg, DrawAttributes ignoreAttributes)
    {
        Reset();
        _svg = new SKSvg
        {
            IgnoreAttributes = ignoreAttributes
        };

        Picture = _svg.FromSvg(svg);
        Model = _svg.Model;
        if (Picture is { } && Model is { })
        {
            return Picture;
        }

        return null;
    }
}
