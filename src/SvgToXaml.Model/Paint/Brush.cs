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
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Model.Paint;

public abstract record Brush : Resource
{
    public SkiaSharp.SKRect Bounds { get; init; }

    public SkiaSharp.SKMatrix? LocalMatrix { get; init; }
  
    public SkiaSharp.SKMatrix WithTransXY(SkiaSharp.SKMatrix matrix, float x, float y)
    {
        return new SkiaSharp.SKMatrix(
            matrix.ScaleX,
            matrix.SkewX,
            matrix.TransX - x,
            matrix.SkewY,
            matrix.ScaleY,
            matrix.TransY - y,
            matrix.Persp0,
            matrix.Persp1,
            matrix.Persp2);
    }
}
