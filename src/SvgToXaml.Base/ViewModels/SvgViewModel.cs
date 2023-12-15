/*
 * SvgToXaml A Svg to Xaml converter.
 * Copyright (C) 2023  Wiesław Šoltés
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System.IO;
using Svg.Model;
using ShimSkiaSharp;
using Svg.Skia;

namespace SvgToXaml.ViewModels;

public class SvgViewModel : ViewModelBase
{
    private static readonly SkiaModel s_model;
    private static readonly IAssetLoader s_assetLoader;

    public SKDrawable? Drawable { get; private set; }

    public SKPicture? Model { get; private set; }

    public SkiaSharp.SKPicture? Picture { get; private set; }

    static SvgViewModel()
    {
        s_model = new SkiaModel(new SKSvgSettings());
        s_assetLoader = new SkiaAssetLoader(new SkiaModel(new SKSvgSettings()));
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
        var svgDocument = SvgExtensions.Open(stream);
        if (svgDocument is { })
        {
            Model = SvgExtensions.ToModel(svgDocument, s_assetLoader, out var drawable, out _, ignoreAttributes);
            Drawable = drawable;
            Picture = s_model.ToSKPicture(Model);
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
            Picture = s_model.ToSKPicture(Model);
            return Picture;
        }
        return null;
    }
}
