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
using System.Globalization;
using System.Linq;
using System.Text;
using SvgToXaml.Model.Containers;
using SvgToXaml.Model.Drawing;
using SvgToXaml.Model.Paint;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Converter;

public class XamlGenerator
{
    private static string ToKey(string? key)
    {
        return key is null ? "" : $" x:Key=\"{key}\"";
    }

    private static string ToGradientSpreadMethod(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
    {
        return shaderTileMode switch
        {
            ShimSkiaSharp.SKShaderTileMode.Clamp => "Pad",
            ShimSkiaSharp.SKShaderTileMode.Repeat => "Repeat",
            ShimSkiaSharp.SKShaderTileMode.Mirror => "Reflect",
            _ => "Pad"
        };
    }

    private static string ToTileMode(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
    {
        return shaderTileMode switch
        {
            ShimSkiaSharp.SKShaderTileMode.Clamp => "None",
            ShimSkiaSharp.SKShaderTileMode.Repeat => "Tile",
            ShimSkiaSharp.SKShaderTileMode.Mirror => "FlipXY",
            _ => "None"
        };
    }

    private static string ToPenLineCap(ShimSkiaSharp.SKStrokeCap strokeCap)
    {
        return strokeCap switch
        {
            ShimSkiaSharp.SKStrokeCap.Butt => "Flat",
            ShimSkiaSharp.SKStrokeCap.Round => "Round",
            ShimSkiaSharp.SKStrokeCap.Square => "Square",
            _ => "Flat"
        };
    }

    private static string ToPenLineJoin(ShimSkiaSharp.SKStrokeJoin strokeJoin, bool useCompatMode = false)
    {
        return strokeJoin switch
        {
            ShimSkiaSharp.SKStrokeJoin.Miter => "Miter",
            ShimSkiaSharp.SKStrokeJoin.Round => "Round",
            ShimSkiaSharp.SKStrokeJoin.Bevel => "Bevel",
            _ => useCompatMode ? "Miter" : "Bevel"
        };
    }

    private static string ToXamlString(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string ToXamlString(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string ToHexColor(ShimSkiaSharp.SKColor skColor)
    {
        var sb = new StringBuilder();
        sb.Append('#');
        sb.AppendFormat("{0:X2}", skColor.Alpha);
        sb.AppendFormat("{0:X2}", skColor.Red);
        sb.AppendFormat("{0:X2}", skColor.Green);
        sb.AppendFormat("{0:X2}", skColor.Blue);
        return sb.ToString();
    }

    private static string ToPoint(SkiaSharp.SKPoint skPoint)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(skPoint.X));
        sb.Append(',');
        sb.Append(ToXamlString(skPoint.Y));
        return sb.ToString();
    }

    private static string ToRect(ShimSkiaSharp.SKRect sKRect)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(sKRect.Left));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Top));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Width));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Height));
        return sb.ToString();
    }

    private static string ToMatrix(SkiaSharp.SKMatrix skMatrix)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(skMatrix.ScaleX));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.SkewY));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.SkewX));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.ScaleY));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.TransX));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.TransY));
        return sb.ToString();
    }

    private static string ToSvgPathData(SkiaSharp.SKPath path, SkiaSharp.SKMatrix matrix)
    {
        var transformedPath = new SkiaSharp.SKPath(path);
        transformedPath.Transform(matrix);
        if (transformedPath.FillType == SkiaSharp.SKPathFillType.EvenOdd)
        {
            // EvenOdd
            var sb = new StringBuilder();
            sb.Append("F0 ");
            sb.Append(transformedPath.ToSvgPathData());
            return sb.ToString();
        }
        else
        {
            // Nonzero 
            var sb = new StringBuilder();
            sb.Append("F1 ");
            sb.Append(transformedPath.ToSvgPathData());
            return sb.ToString();
        }
    }

    private string GenerateBrush(Brush brush, XamlGeneratorSettings settings)
    {
        return brush switch
        {
            SolidColorBrush solidColorBrush => GenerateSolidColorBrush(solidColorBrush, settings),
            LinearGradientBrush linearGradientBrush => GenerateLinearGradientBrush(linearGradientBrush, settings),
            RadialGradientBrush radialGradientBrush => GenerateRadialGradientBrush(radialGradientBrush, settings),
            TwoPointConicalGradientBrush twoPointConicalGradientBrush => GenerateTwoPointConicalGradientBrush(twoPointConicalGradientBrush, settings),
            PictureBrush pictureBrush => GeneratePictureBrush(pictureBrush, settings),
            _ => ""
        };
    }

    private string GenerateSolidColorBrush(SolidColorBrush solidColorBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        sb.Append($"<SolidColorBrush{ToKey(solidColorBrush.Key)}");
        sb.Append($" Color=\"{ToHexColor(solidColorBrush.Color)}\"");
        sb.Append($"/>{settings.NewLine}");

        return sb.ToString();
    }

    private string GenerateLinearGradientBrush(LinearGradientBrush linearGradientBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        var start = linearGradientBrush.Start;
        var end = linearGradientBrush.End;

        sb.Append($"<LinearGradientBrush{ToKey(linearGradientBrush.Key)}");

        sb.Append($" StartPoint=\"{ToPoint(start)}\"");
        sb.Append($" EndPoint=\"{ToPoint(end)}\"");

        if (linearGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientBrush.Mode)}\"");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($" MappingMode=\"Absolute\"");
        }

        sb.Append($">{settings.NewLine}");

        if (linearGradientBrush.LocalMatrix is { })
        {
            var localMatrix = linearGradientBrush.LocalMatrix.Value;

            sb.Append($"  <LinearGradientBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </LinearGradientBrush.Transform>{settings.NewLine}");
        }

        if (linearGradientBrush.GradientStops.Count > 0)
        {
            sb.Append($"  <LinearGradientBrush.GradientStops>{settings.NewLine}");

            foreach (var stop in linearGradientBrush.GradientStops)
            {
                var color = ToHexColor(stop.Color);
                var offset = ToXamlString(stop.Offset);
                sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{settings.NewLine}");
            }

            sb.Append($"  </LinearGradientBrush.GradientStops>{settings.NewLine}");
        }

        sb.Append($"</LinearGradientBrush>{settings.NewLine}");

        return sb.ToString();
    }

    private string GenerateRadialGradientBrush(RadialGradientBrush radialGradientBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        var radius = radialGradientBrush.Radius;

        var center = radialGradientBrush.Center;
        var gradientOrigin = radialGradientBrush.Center;

        if (!settings.UseCompatMode)
        {
            radius = radius / radialGradientBrush.Bounds.Width;
        }

        sb.Append($"<RadialGradientBrush{ToKey(radialGradientBrush.Key)}");

        sb.Append($" Center=\"{ToPoint(center)}\"");
        sb.Append($" GradientOrigin=\"{ToPoint(gradientOrigin)}\"");

        if (settings.UseCompatMode)
        {
            sb.Append($" RadiusX=\"{ToXamlString(radius)}\"");
            sb.Append($" RadiusY=\"{ToXamlString(radius)}\"");
        }
        else
        {
            sb.Append($" Radius=\"{ToXamlString(radius)}\"");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($" MappingMode=\"Absolute\"");
        }

        if (radialGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(radialGradientBrush.Mode)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (radialGradientBrush.LocalMatrix is { })
        {
            var localMatrix = radialGradientBrush.LocalMatrix.Value;

            sb.Append($"  <RadialGradientBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </RadialGradientBrush.Transform>{settings.NewLine}");
        }

        if (radialGradientBrush.GradientStops.Count > 0)
        {
            sb.Append($"  <RadialGradientBrush.GradientStops>{settings.NewLine}");

            foreach (var stop in radialGradientBrush.GradientStops)
            {
                var color = ToHexColor(stop.Color);
                var offset = ToXamlString(stop.Offset);
                sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{settings.NewLine}");
            }

            sb.Append($"  </RadialGradientBrush.GradientStops>{settings.NewLine}");
        }

        sb.Append($"</RadialGradientBrush>{settings.NewLine}");

        return sb.ToString();
    }

    private string GenerateTwoPointConicalGradientBrush(TwoPointConicalGradientBrush twoPointConicalGradientBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
        // ReSharper disable once UnusedVariable
        var startRadius = twoPointConicalGradientBrush.StartRadius;

        // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
        // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
        var endRadius = twoPointConicalGradientBrush.EndRadius;

        var center = twoPointConicalGradientBrush.End;
        var gradientOrigin = twoPointConicalGradientBrush.Start;

        if (!settings.UseCompatMode)
        {
            endRadius = endRadius / twoPointConicalGradientBrush.Bounds.Width;
        }

        sb.Append($"<RadialGradientBrush{ToKey(twoPointConicalGradientBrush.Key)}");

        sb.Append($" Center=\"{ToPoint(center)}\"");
        sb.Append($" GradientOrigin=\"{ToPoint(gradientOrigin)}\"");

        if (settings.UseCompatMode)
        {
            sb.Append($" RadiusX=\"{ToXamlString(endRadius)}\"");
            sb.Append($" RadiusY=\"{ToXamlString(endRadius)}\"");
        }
        else
        {
            sb.Append($" Radius=\"{ToXamlString(endRadius)}\"");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($" MappingMode=\"Absolute\"");
        }

        if (twoPointConicalGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(twoPointConicalGradientBrush.Mode)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (twoPointConicalGradientBrush.LocalMatrix is { })
        {
            var localMatrix = twoPointConicalGradientBrush.LocalMatrix.Value;

            sb.Append($"  <RadialGradientBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </RadialGradientBrush.Transform>{settings.NewLine}");
        }

        if (twoPointConicalGradientBrush.GradientStops.Count > 0)
        {
            sb.Append($"  <RadialGradientBrush.GradientStops>{settings.NewLine}");

            foreach (var stop in twoPointConicalGradientBrush.GradientStops)
            {
                var color = ToHexColor(stop.Color);
                var offset = ToXamlString(stop.Offset);
                sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{settings.NewLine}");
            }

            sb.Append($"  </RadialGradientBrush.GradientStops>{settings.NewLine}");
        }

        sb.Append($"</RadialGradientBrush>{settings.NewLine}");

        return sb.ToString();
    }

    private string GeneratePictureBrush(PictureBrush pictureBrush, XamlGeneratorSettings settings)
    {
        if (pictureBrush.Picture is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        var sourceRect = pictureBrush.CullRect;
        var destinationRect = pictureBrush.Tile;

        // TODO: Use different visual then Image ?
        sb.Append($"<VisualBrush{ToKey(pictureBrush.Key)}");

        if (pictureBrush.TileMode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" TileMode=\"{ToTileMode(pictureBrush.TileMode)}\"");
        }

        if (settings.UseCompatMode)
        {
            if (!sourceRect.IsEmpty)
            {
                sb.Append($" Viewport=\"{ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
            }

            if (!destinationRect.IsEmpty)
            {
                sb.Append($" Viewbox=\"{ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
            }
        }
        else
        {
            if (!sourceRect.IsEmpty)
            {
                sb.Append($" SourceRect=\"{ToRect(sourceRect)}\"");
            }

            if (!destinationRect.IsEmpty)
            {
                sb.Append($" DestinationRect=\"{ToRect(destinationRect)}\"");
            }
        }

        sb.Append($">{settings.NewLine}");

        if (pictureBrush.LocalMatrix is { })
        {
            var localMatrix = pictureBrush.LocalMatrix.Value;

            sb.Append($"  <VisualBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </VisualBrush.Transform>{settings.NewLine}");
        }

        if (pictureBrush.Picture is not null)
        {
            sb.Append($"  <VisualBrush.Visual>{settings.NewLine}");
            sb.Append(GenerateImage(pictureBrush.Picture, settings with { WriteResources = false, AddTransparentBackground = false }, SkiaSharp.SKMatrix.CreateIdentity()));
            sb.Append($"{settings.NewLine}");
            sb.Append($"  </VisualBrush.Visual>{settings.NewLine}");
        }

        sb.Append($"</VisualBrush>{settings.NewLine}");

        return sb.ToString();
    }

    private string GeneratePen(Pen pen, XamlGeneratorSettings settings)
    {
        if (pen.Brush is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        sb.Append($"<Pen{ToKey(pen.Key)}");

        if (pen.Brush is SolidColorBrush solidColorBrush)
        {
            sb.Append($" Brush=\"{ToHexColor(solidColorBrush.Color)}\"");
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (pen.StrokeWidth != 1.0)
        {
            sb.Append($" Thickness=\"{ToXamlString(pen.StrokeWidth)}\"");
        }

        if (pen.StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
        {
            if (settings.UseCompatMode)
            {
                sb.Append($" StartLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                sb.Append($" EndLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
            }
            else
            {
                sb.Append($" LineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
            }
        }

        if (settings.UseCompatMode)
        {
            if (pen.Dashes is { Intervals: { } })
            {
                if (pen.StrokeCap != ShimSkiaSharp.SKStrokeCap.Square)
                {
                    sb.Append($" DashCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                }
            }

            if (pen.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Miter)
            {
                sb.Append($" LineJoin=\"{ToPenLineJoin(pen.StrokeJoin)}\"");
            }
        }
        else
        {
            if (pen.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
            {
                sb.Append($" LineJoin=\"{ToPenLineJoin(pen.StrokeJoin)}\"");
            }
        }

        if (settings.UseCompatMode)
        {
            var miterLimit = pen.StrokeMiter;
            var strokeWidth = pen.StrokeWidth;

            if (miterLimit < 1.0f)
            {
                miterLimit = 10.0f;
            }
            else
            {
                if (strokeWidth <= 0.0f)
                {
                    miterLimit = 1.0f;
                }
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (miterLimit != 10.0)
            {
                sb.Append($" MiterLimit=\"{ToXamlString(miterLimit)}\"");
            }
        }
        else
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (pen.StrokeMiter != 10.0)
            {
                sb.Append($" MiterLimit=\"{ToXamlString(pen.StrokeMiter)}\"");
            }
        }

        if (pen.Brush is not SolidColorBrush || pen.Dashes is { Intervals: { } })
        {
            sb.Append($">{settings.NewLine}");
        }
        else
        {
            sb.Append($"/>{settings.NewLine}");
        }

        if (pen.Dashes is { Intervals: { } })
        {
            var dashes = new List<double>();

            foreach (var interval in pen.Dashes.Intervals)
            {
                dashes.Add(interval / pen.StrokeWidth);
            }

            var offset = pen.Dashes.Phase / pen.StrokeWidth;

            sb.Append($"  <Pen.DashStyle>{settings.NewLine}");
            sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(ToXamlString))}\" Offset=\"{ToXamlString(offset)}\"/>{settings.NewLine}");
            sb.Append($"  </Pen.DashStyle>{settings.NewLine}");
        }

        if (pen.Brush is not SolidColorBrush)
        {
            sb.Append($"  <Pen.Brush>{settings.NewLine}");
            sb.Append(GenerateBrush(pen.Brush, settings));
            sb.Append($"  </Pen.Brush>{settings.NewLine}");
        }

        if (pen.Brush is not SolidColorBrush || pen.Dashes is { Intervals: { } })
        {
            sb.Append($"</Pen>{settings.NewLine}");
        }

        return sb.ToString();
    }

    private string GenerateDrawing(Drawing drawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        return drawing switch
        {
            GeometryDrawing geometryDrawing => GenerateGeometryDrawing(geometryDrawing, settings, matrix),
            DrawingGroup drawingGroup => GenerateDrawingGroup(drawingGroup, settings),
            DrawingImage drawingImage => GenerateDrawingImage(drawingImage, settings, matrix),
            _ => ""
        };
    }

    private string GenerateGeometryDrawing(GeometryDrawing geometryDrawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (geometryDrawing.Paint is null || geometryDrawing.Geometry is null)
        {
            return "";
        }
 
        var sb = new StringBuilder();
            
        sb.Append($"<GeometryDrawing");

        var isFilled = geometryDrawing.Brush is { };
        var isStroked = geometryDrawing.Pen is { };

        if (isFilled && geometryDrawing.Brush is SolidColorBrush solidColorBrush && settings.Resources is null)
        {
            sb.Append($" Brush=\"{ToHexColor(solidColorBrush.Color)}\"");
        }

        var brush = default(Brush);
        var pen = default(Pen);

        if (isFilled && geometryDrawing.Brush is { } and not SolidColorBrush && settings.Resources is null)
        {
            brush =geometryDrawing. Brush;
        }

        if (isFilled && geometryDrawing.Paint is { } && settings.Resources is { })
        {
            bool haveBrush = false;

            if (settings.ReuseExistingResources)
            {
                var existingBrush = settings.Resources.Brushes.FirstOrDefault(x =>
                {
                    if (x.Value.Paint.Shader is { } && x.Value.Paint.Shader.Equals(geometryDrawing.Paint.Shader))
                    {
                        return true;
                    }

                    return false;
                });

                if (!string.IsNullOrEmpty(existingBrush.Key))
                {
                    settings.Resources.UseBrushes.Add(existingBrush.Value.Brush);
                    sb.Append($" Brush=\"{{DynamicResource {existingBrush.Key}}}\"");
                    haveBrush = true;
                }
            }

            if (!haveBrush)
            {
                if (geometryDrawing.Brush is { } && settings.Resources is { } && geometryDrawing.Brush.Key is { })
                {
                    settings.Resources.UseBrushes.Add(geometryDrawing.Brush);
                    sb.Append($" Brush=\"{{DynamicResource {geometryDrawing.Brush.Key}}}\"");
                    haveBrush = true;
                }
                    
                if (!haveBrush)
                {
                    brush = geometryDrawing.Brush;
                }
            }
        }

        if (isStroked && geometryDrawing.Pen is { } && settings.Resources is null)
        {
            pen = geometryDrawing.Pen;
        }

        if (isStroked && geometryDrawing.Paint is { } && settings.Resources is { })
        {
            bool havePen = false;

            if (settings.ReuseExistingResources)
            {
                var existingPen = settings.Resources.Pens.FirstOrDefault(x =>
                {
                    if (x.Value.Paint.Shader is { } 
                        && x.Value.Paint.Shader.Equals(geometryDrawing.Paint.Shader)
                        && x.Value.Paint.StrokeWidth.Equals(geometryDrawing.Paint.StrokeWidth)
                        && x.Value.Paint.StrokeCap.Equals(geometryDrawing.Paint.StrokeCap)
                        && x.Value.Paint.PathEffect == geometryDrawing.Paint.PathEffect
                        && x.Value.Paint.StrokeJoin.Equals(geometryDrawing.Paint.StrokeJoin)
                        && x.Value.Paint.StrokeMiter.Equals(geometryDrawing.Paint.StrokeMiter))
                    {
                        return true;
                    }

                    return false;
                });

                if (!string.IsNullOrEmpty(existingPen.Key))
                {
                    settings.Resources.UsePens.Add(existingPen.Value.Pen);
                    sb.Append($" Pen=\"{{DynamicResource {existingPen.Key}}}\"");
                    havePen = true;
                }
            }

            if (!havePen)
            {
                if (geometryDrawing.Pen is { } && settings.Resources is { } && geometryDrawing.Pen.Key is { })
                {
                    settings.Resources.UsePens.Add(geometryDrawing.Pen);
                    sb.Append($" Pen=\"{{DynamicResource {geometryDrawing.Pen.Key}}}\"");
                    havePen = true;
                }

                if (!havePen)
                {
                    pen = geometryDrawing.Pen;
                }
            }
        }

        if (geometryDrawing.Geometry is { })
        {
            sb.Append($" Geometry=\"{ToSvgPathData(geometryDrawing.Geometry, matrix ?? SkiaSharp.SKMatrix.CreateIdentity())}\"");
        }

        if (brush is { } || pen is { })
        {
            sb.Append($">{settings.NewLine}");
        }
        else
        {
            sb.Append($"/>{settings.NewLine}");
        }

        if (brush is { })
        {
            sb.Append($"  <GeometryDrawing.Brush>{settings.NewLine}");
            sb.Append(GenerateBrush(brush, settings));
            sb.Append($"  </GeometryDrawing.Brush>{settings.NewLine}");
        }

        if (pen is { })
        {
            sb.Append($"  <GeometryDrawing.Pen>{settings.NewLine}");
            sb.Append(GeneratePen(pen, settings));
            sb.Append($"  </GeometryDrawing.Pen>{settings.NewLine}");
        }

        if (brush is { } || pen is { })
        {
            sb.Append($"</GeometryDrawing>{settings.NewLine}");
        }

        return sb.ToString();
    }

    public string GenerateDrawingGroup(DrawingGroup drawingGroup, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        sb.Append($"<DrawingGroup{ToKey(drawingGroup.Key)}");

        if (drawingGroup.Opacity is { })
        {
            sb.Append($" Opacity=\"{ToXamlString(drawingGroup.Opacity.Value)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (drawingGroup.ClipGeometry is { })
        {
            var clipGeometry = ToSvgPathData(drawingGroup.ClipGeometry, SkiaSharp.SKMatrix.CreateIdentity());

            sb.Append($"  <DrawingGroup.ClipGeometry>{settings.NewLine}");
            sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{settings.NewLine}");
            sb.Append($"  </DrawingGroup.ClipGeometry>{settings.NewLine}");
        }

        if (drawingGroup.Transform is { } && !settings.TransformGeometry)
        {
            var matrix = drawingGroup.Transform.Value;

            sb.Append($"  <DrawingGroup.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{settings.NewLine}");
            sb.Append($"  </DrawingGroup.Transform>{settings.NewLine}");
        }

        if (drawingGroup.OpacityMask is { })
        {
            sb.Append($"<DrawingGroup.OpacityMask>{settings.NewLine}");
            sb.Append(GenerateBrush(drawingGroup.OpacityMask, settings));
            sb.Append($"</DrawingGroup.OpacityMask>{settings.NewLine}");
        }

        if (settings.AddTransparentBackground && drawingGroup.Picture is { })
        {
            var left = drawingGroup.Picture.CullRect.Left;
            var top = drawingGroup.Picture.CullRect.Top;
            var right = drawingGroup.Picture.CullRect.Right;
            var bottom = drawingGroup.Picture.CullRect.Bottom;
            sb.Append($"<GeometryDrawing Brush=\"Transparent\" ");
            sb.Append($"Geometry=\"F1");
            sb.Append($"M{ToXamlString(left)},{ToXamlString(top)}");
            sb.Append($"L{ToXamlString(right)},{ToXamlString(top)}");
            sb.Append($"L{ToXamlString(right)},{ToXamlString(bottom)}");
            sb.Append($"L{ToXamlString(left)},{ToXamlString(bottom)}");
            sb.Append($"z\" ");
            sb.Append($"/>{settings.NewLine}");
        }

        foreach (var child in drawingGroup.Children)
        {
            sb.Append(GenerateDrawing(child, settings, settings.TransformGeometry ? drawingGroup.Transform : null));
        }

        sb.Append($"</DrawingGroup>");

        return sb.ToString();
    }

    private string GenerateDrawingImage(DrawingImage drawingImage, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (drawingImage.Drawing is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        sb.Append($"<DrawingImage>{settings.NewLine}");

        if (settings.UseCompatMode)
        {
            sb.Append($"  <DrawingImage.Drawing>{settings.NewLine}");
        }

        sb.Append(GenerateDrawing(drawingImage.Drawing, settings, matrix));

        if (settings.UseCompatMode)
        {
            sb.Append($"  </DrawingImage.Drawing>{settings.NewLine}");
        }

        sb.Append($"</DrawingImage>{settings.NewLine}");

        return sb.ToString();
    }

    private string GenerateResource(Resource resource, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        return resource switch
        {
            SolidColorBrush solidColorBrush => GenerateSolidColorBrush(solidColorBrush, settings),
            LinearGradientBrush linearGradientBrush => GenerateLinearGradientBrush(linearGradientBrush, settings),
            RadialGradientBrush radialGradientBrush => GenerateRadialGradientBrush(radialGradientBrush, settings),
            TwoPointConicalGradientBrush twoPointConicalGradientBrush => GenerateTwoPointConicalGradientBrush(twoPointConicalGradientBrush, settings),
            PictureBrush pictureBrush => GeneratePictureBrush(pictureBrush, settings),
            Pen pen => GeneratePen(pen, settings),
            GeometryDrawing geometryDrawing => GenerateGeometryDrawing(geometryDrawing, settings, matrix),
            DrawingGroup drawingGroup => GenerateDrawingGroup(drawingGroup, settings),
            DrawingImage drawingImage => GenerateDrawingImage(drawingImage, settings, matrix),
            Image image => GenerateImage(image, settings, matrix),
            _ => ""
        };
    }

    private string GenerateResourceDictionary(ResourceDictionary resourceDictionary, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        if (settings.ReuseExistingResources)
        {
            foreach (var resource in resourceDictionary.UseBrushes)
            {
                sb.Append(GenerateBrush(resource, settings));
            }

            foreach (var resource in resourceDictionary.UsePens)
            {
                sb.Append(GeneratePen(resource, settings));
            }
        }
        else
        {
            foreach (var resource in resourceDictionary.Brushes)
            {
                sb.Append(GenerateBrush(resource.Value.Brush, settings));
            }

            foreach (var resource in resourceDictionary.Pens)
            {
                sb.Append(GeneratePen(resource.Value.Pen, settings));
            }               
        }

        return sb.ToString();
    }

    public string GenerateImage(Image image, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (image.Source is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        var content = GenerateDrawingImage(image.Source, settings, matrix);

        sb.Append($"<Image{ToKey(image.Key)}");

        if (settings.Resources is { } && (settings.Resources.Brushes.Count > 0 || settings.Resources.Pens.Count > 0) && settings.WriteResources)
        {
            // sb.Append(settings.UseCompatMode
            //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
            //     : $" xmlns=\"https://github.com/avaloniaui\"");
            // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        }

        sb.Append($">{settings.NewLine}");

        if (settings.Resources is { } && (settings.Resources.Brushes.Count > 0 || settings.Resources.Pens.Count > 0) && settings.WriteResources)
        {
            sb.Append($"<Image.Resources>{settings.NewLine}");
            sb.Append(GenerateResourceDictionary(settings.Resources, settings with { WriteResources = false, AddTransparentBackground = false }));
            sb.Append($"</Image.Resources>{settings.NewLine}");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($"<Image.Source>{settings.NewLine}");
        }

        sb.Append(content);

        if (settings.UseCompatMode)
        {
            sb.Append($"</Image.Source>{settings.NewLine}");
        }

        sb.Append($"</Image>");

        return sb.ToString();
    }

    public string GenerateStyles(Styles styles, XamlGeneratorSettings settings)
    {
        if (styles.Resources is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        var content = new StringBuilder();

        foreach (var result in styles.Resources)
        {
            content.Append(GenerateResource(result, settings with { WriteResources = false, AddTransparentBackground = false }, SkiaSharp.SKMatrix.CreateIdentity()));
            content.Append(settings.NewLine);
        }

        if (settings.UseCompatMode)
        {
            sb.Append($"<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"{settings.NewLine}");
            sb.Append($"                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{settings.NewLine}");
        }
        else
        {
            sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{settings.NewLine}");
            sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{settings.NewLine}");
        }

        if (styles.GeneratePreview && !settings.UseCompatMode)
        {
            sb.Append($"  <Design.PreviewWith>{settings.NewLine}");
            sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">{settings.NewLine}");
            sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">{settings.NewLine}");

            foreach (var result in styles.Resources)
            {
                if (styles.GenerateImage)
                {
                    sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>{settings.NewLine}");
                }
                else
                {
                    sb.Append($"        <Image>{settings.NewLine}");

                    if (settings.UseCompatMode)
                    {
                        sb.Append($"            <Image.Source>{settings.NewLine}");
                    }

                    sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>{settings.NewLine}");

                    if (settings.UseCompatMode)
                    {
                        sb.Append($"            </Image.Source>{settings.NewLine}");
                    }

                    sb.Append($"        </Image>{settings.NewLine}");
                }
            }

            sb.Append($"      </WrapPanel>{settings.NewLine}");
            sb.Append($"    </ScrollViewer>{settings.NewLine}");
            sb.Append($"  </Design.PreviewWith>{settings.NewLine}");
        }

        if (!settings.UseCompatMode)
        {
            sb.Append($"  <Style>{settings.NewLine}");
            sb.Append($"    <Style.Resources>{settings.NewLine}");
        }

        if (settings.Resources is { } && (settings.Resources.Brushes.Count > 0 || settings.Resources.Pens.Count > 0))
        {
            sb.Append(GenerateResourceDictionary(settings.Resources, settings with { WriteResources = false, AddTransparentBackground = false }));
        }

        sb.Append(content);

        if (settings.UseCompatMode)
        {
            sb.Append($"</ResourceDictionary>");
        }
        else
        {
            sb.Append($"    </Style.Resources>{settings.NewLine}");
            sb.Append($"  </Style>{settings.NewLine}");
            sb.Append($"</Styles>{settings.NewLine}");
        }

        return sb.ToString();
    }
}
