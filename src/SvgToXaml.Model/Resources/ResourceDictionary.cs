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
using System.Collections.Generic;
using SvgToXaml.Model.Paint;

namespace SvgToXaml.Model.Resources;

public record ResourceDictionary
{
    public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, Brush Brush)> Brushes  { get; init; } = new();

    public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, Pen Pen)> Pens  { get; init; } = new();

    public int BrushCounter { get; set; }

    public int PenCounter { get; set; }

    public HashSet<Brush> UseBrushes { get; init; } = new();

    public HashSet<Pen> UsePens { get; init; } = new();
}
