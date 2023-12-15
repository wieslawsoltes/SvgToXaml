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

public record Pen : Resource
{
    public SkiaSharp.SKRect Bounds { get; init; }

    public Brush? Brush { get; init; }

    public float StrokeWidth { get; init; }

    public ShimSkiaSharp.SKStrokeCap StrokeCap { get; init; }

    public ShimSkiaSharp.SKStrokeJoin StrokeJoin { get; init; }

    public float StrokeMiter { get; init; }

    public Dashes? Dashes { get; init; }
}
