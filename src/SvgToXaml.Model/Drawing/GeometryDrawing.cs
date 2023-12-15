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
using SvgToXaml.Model.Paint;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Model.Drawing;

public record GeometryDrawing : Drawing
{
    public ShimSkiaSharp.SKPaint? Paint { get; }

    public SkiaSharp.SKPath? Geometry { get; }

    public Brush? Brush { get; }

    public Pen? Pen { get; }

    public GeometryDrawing(
        ShimSkiaSharp.SKPaint? paint = null, 
        SkiaSharp.SKPath? geometry = null, 
        ResourceDictionary? resources = null)
    {
        Paint = paint;
        Geometry = geometry;

        if (Paint is { } && Geometry is { })
        {
            var isFilled = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Fill;

            if (isFilled && Paint.Shader is { })
            {
                var resourceKey = resources is { } ? $"Brush{resources.BrushCounter++}" : null;
                    
                Brush = Factory.CreateBrush(Paint.Shader, Geometry.Bounds, resourceKey);

                if (resources is { } && Brush?.Key is { })
                {
                    resources.Brushes.Add(Brush.Key, (Paint, Brush));
                }
            }

            var isStroked = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Stroke;

            if (isStroked && Paint.Shader is { })
            {
                var resourceKey = resources is { } ? $"Pen{resources.PenCounter++}" : null;

                Pen = Factory.CreatePen(Paint, Geometry.Bounds, resourceKey);

                if (resources is { } && Pen?.Key is { })
                {
                    resources.Pens.Add(Pen.Key, (Paint, Pen));
                }
            }
        }
    }
}
