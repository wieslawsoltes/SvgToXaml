using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SvgToXamlConverter
{
    public class Resources
    {
        public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, string Resource)> Brushes  { get; set; } = new();

        public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, string Resource)> Pens  { get; set; } = new();

        public int BrushCounter { get; set; }

        public int PenCounter { get; set; }
    }

    public class SvgConverter
    {
        public bool UseCompatMode { get; set; } = false;

        public bool UseBrushTransform { get; set; } = false;

        public string NewLine { get; set; } = "\r\n";

        private const byte OpaqueAlpha = 255;

        private static readonly ShimSkiaSharp.SKColor s_transparentBlack = new(0, 0, 0, 255);

        private enum LayerType
        {
            UnknownPaint,
            MaskGroup,
            MaskBrush,
            OpacityGroup,
            FilterGroup
        }

        private string ToKey(string? key)
        {
            return key is null ? "" : $" x:Key=\"{key}\"";
        }

        private string ToGradientSpreadMethod(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
        {
            return shaderTileMode switch
            {
                ShimSkiaSharp.SKShaderTileMode.Clamp => "Pad",
                ShimSkiaSharp.SKShaderTileMode.Repeat => "Repeat",
                ShimSkiaSharp.SKShaderTileMode.Mirror => "Reflect",
                _ => "Pad"
            };
        }

        private string ToTileMode(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
        {
            return shaderTileMode switch
            {
                ShimSkiaSharp.SKShaderTileMode.Clamp => "None",
                ShimSkiaSharp.SKShaderTileMode.Repeat => "Tile",
                ShimSkiaSharp.SKShaderTileMode.Mirror => "FlipXY",
                _ => "None"
            };
        }

        private string ToPenLineCap(ShimSkiaSharp.SKStrokeCap strokeCap)
        {
            return strokeCap switch
            {
                ShimSkiaSharp.SKStrokeCap.Butt => "Flat",
                ShimSkiaSharp.SKStrokeCap.Round => "Round",
                ShimSkiaSharp.SKStrokeCap.Square => "Square",
                _ => "Flat"
            };
        }

        private string ToPenLineJoin(ShimSkiaSharp.SKStrokeJoin strokeJoin)
        {
            return strokeJoin switch
            {
                ShimSkiaSharp.SKStrokeJoin.Miter => "Miter",
                ShimSkiaSharp.SKStrokeJoin.Round => "Round",
                ShimSkiaSharp.SKStrokeJoin.Bevel => "Bevel",
                _ => UseCompatMode ? "Miter" : "Bevel"
            };
        }

        private string ToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string ToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string ToHexColor(ShimSkiaSharp.SKColor skColor)
        {
            var sb = new StringBuilder();
            sb.Append('#');
            sb.AppendFormat("{0:X2}", skColor.Alpha);
            sb.AppendFormat("{0:X2}", skColor.Red);
            sb.AppendFormat("{0:X2}", skColor.Green);
            sb.AppendFormat("{0:X2}", skColor.Blue);
            return sb.ToString();
        }

        private string ToPoint(SkiaSharp.SKPoint skPoint)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skPoint.X));
            sb.Append(',');
            sb.Append(ToString(skPoint.Y));
            return sb.ToString();
        }

        private string ToRect(ShimSkiaSharp.SKRect sKRect)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(sKRect.Left));
            sb.Append(',');
            sb.Append(ToString(sKRect.Top));
            sb.Append(',');
            sb.Append(ToString(sKRect.Width));
            sb.Append(',');
            sb.Append(ToString(sKRect.Height));
            return sb.ToString();
        }

        private string ToMatrix(SkiaSharp.SKMatrix skMatrix)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skMatrix.ScaleX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.ScaleY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransY));
            return sb.ToString();
        }

        private string ToSvgPathData(SkiaSharp.SKPath path)
        {
            if (path.FillType == SkiaSharp.SKPathFillType.EvenOdd)
            {
                // EvenOdd
                var sb = new StringBuilder();
                sb.Append("F0 ");
                sb.Append(path.ToSvgPathData());
                return sb.ToString();
            }
            else
            {
                // Nonzero 
                var sb = new StringBuilder();
                sb.Append("F1 ");
                sb.Append(path.ToSvgPathData());
                return sb.ToString();
            }
        }

        private SkiaSharp.SKMatrix AdjustMatrixLocation(SkiaSharp.SKMatrix matrix, float x, float y)
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

        private string ToBrush(ShimSkiaSharp.ColorShader colorShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var sb = new StringBuilder();
            sb.Append($"<SolidColorBrush{ToKey(key)}");
            sb.Append($" Color=\"{ToHexColor(colorShader.Color)}\"");
            sb.Append($"/>{NewLine}");
            return sb.ToString();
        }

        private string ToBrush(ShimSkiaSharp.LinearGradientShader linearGradientShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var sb = new StringBuilder();

            var start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start);
            var end = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End);

            if (!UseBrushTransform)
            {
                if (linearGradientShader.LocalMatrix is { })
                {
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);
                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - skBounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - skBounds.Location.Y);

                    start = localMatrix.MapPoint(start);
                    end = localMatrix.MapPoint(end);
                }
                else
                {
                    start.X = Math.Max(0f, start.X - skBounds.Location.X);
                    start.Y = Math.Max(0f, start.Y - skBounds.Location.Y);
                    end.X = Math.Max(0f, end.X - skBounds.Location.X);
                    end.Y = Math.Max(0f, end.Y - skBounds.Location.Y);
                }
            }
            else
            {
                if (!UseCompatMode)
                {
                    if (linearGradientShader.LocalMatrix is null)
                    {
                        start.X = Math.Max(0f, start.X - skBounds.Location.X);
                        start.Y = Math.Max(0f, start.Y - skBounds.Location.Y);
                        end.X = Math.Max(0f, end.X - skBounds.Location.X);
                        end.Y = Math.Max(0f, end.Y - skBounds.Location.Y);
                    }
                }
            }

            sb.Append($"<LinearGradientBrush{ToKey(key)}");

            sb.Append($" StartPoint=\"{ToPoint(start)}\"");
            sb.Append($" EndPoint=\"{ToPoint(end)}\"");

            if (linearGradientShader.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\"");
            }

            if (UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            sb.Append($">{NewLine}");

            if (UseBrushTransform)
            {
                if (linearGradientShader.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on LinearGradientBrush
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);

                    if (!UseCompatMode)
                    {
                        localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                    }

                    sb.Append($"  <LinearGradientBrush.Transform>{NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}");
                    sb.Append($"  </LinearGradientBrush.Transform>{NewLine}");
                }
            }

            sb.Append($"  <LinearGradientBrush.GradientStops>{NewLine}");

            if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
            {
                for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                {
                    var color = ToHexColor(linearGradientShader.Colors[i]);
                    var offset = ToString(linearGradientShader.ColorPos[i]);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{NewLine}");
                }
            }

            sb.Append($"  </LinearGradientBrush.GradientStops>{NewLine}");
            sb.Append($"</LinearGradientBrush>{NewLine}");

            return sb.ToString();
        }

        private string ToBrush(ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var sb = new StringBuilder();

            // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
            var startRadius = twoPointConicalGradientShader.StartRadius;

            // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
            // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
            var endRadius = twoPointConicalGradientShader.EndRadius;

            var center = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.Start);
            var gradientOrigin = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.End);

            if (!UseBrushTransform)
            {
                if (twoPointConicalGradientShader.LocalMatrix is { })
                {
                    var localMatrix =
                        Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - skBounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - skBounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radius = localMatrix.MapVector(new SkiaSharp.SKPoint(endRadius, 0));
                    endRadius = radius.X;
                }
                else
                {
                    center.X = Math.Max(0f, center.X - skBounds.Location.X);
                    center.Y = Math.Max(0f, center.Y - skBounds.Location.Y);
                    gradientOrigin.X = Math.Max(0f, gradientOrigin.X - skBounds.Location.X);
                    gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - skBounds.Location.Y);
                }
            }
            else
            {
                if (!UseCompatMode)
                {
                    if (twoPointConicalGradientShader.LocalMatrix is null)
                    {
                        center.X = Math.Max(0f, center.X - skBounds.Location.X);
                        center.Y = Math.Max(0f, center.Y - skBounds.Location.Y);
                        gradientOrigin.X = Math.Max(0f, gradientOrigin.X - skBounds.Location.X);
                        gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - skBounds.Location.Y);
                    }
                }
            }

            if (!UseCompatMode)
            {
                endRadius = endRadius / skBounds.Width;
            }

            sb.Append($"<RadialGradientBrush{ToKey(key)}");

            sb.Append($" Center=\"{ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{ToPoint(gradientOrigin)}\"");

            if (UseCompatMode)
            {
                sb.Append($" RadiusX=\"{ToString(endRadius)}\"");
                sb.Append($" RadiusY=\"{ToString(endRadius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{ToString(endRadius)}\"");
            }

            if (UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            if (twoPointConicalGradientShader.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(twoPointConicalGradientShader.Mode)}\"");
            }

            sb.Append($">{NewLine}");

            if (UseBrushTransform)
            {
                if (twoPointConicalGradientShader.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on RadialGradientBrush
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);

                    if (!UseCompatMode)
                    {
                        localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                    }

                    sb.Append($"  <RadialGradientBrush.Transform>{NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}");
                    sb.Append($"  </RadialGradientBrush.Transform>{NewLine}");
                }
            }

            sb.Append($"  <RadialGradientBrush.GradientStops>{NewLine}");

            if (twoPointConicalGradientShader.Colors is { } && twoPointConicalGradientShader.ColorPos is { })
            {
                for (var i = 0; i < twoPointConicalGradientShader.Colors.Length; i++)
                {
                    var color = ToHexColor(twoPointConicalGradientShader.Colors[i]);
                    var offset = ToString(twoPointConicalGradientShader.ColorPos[i]);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{NewLine}");
                }
            }

            sb.Append($"  </RadialGradientBrush.GradientStops>{NewLine}");
            sb.Append($"</RadialGradientBrush>{NewLine}");

            return sb.ToString();
        }

        private string ToBrush(ShimSkiaSharp.PictureShader pictureShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            if (pictureShader?.Src is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            if (!UseBrushTransform)
            {
                if (pictureShader.LocalMatrix is { })
                {
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(pictureShader.LocalMatrix);

                    if (!localMatrix.IsIdentity)
                    {
#if DEBUG
                        sb.Append($"<!-- TODO: Transform: {ToMatrix(localMatrix)} -->{NewLine}");
#endif
                    }
                }
                else
                {
                    // TODO: Adjust using skBounds.Location ?
                }
            }

            var sourceRect = pictureShader.Src.CullRect;
            var destinationRect = pictureShader.Tile;

            // TODO: Use different visual then Image ?
            sb.Append($"<VisualBrush{ToKey(key)}");

            if (pictureShader.TmX != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" TileMode=\"{ToTileMode(pictureShader.TmX)}\"");
            }

            if (UseCompatMode)
            {
                sb.Append($" Viewport=\"{ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                sb.Append($" Viewbox=\"{ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
            }
            else
            {
                sb.Append($" SourceRect=\"{ToRect(sourceRect)}\"");
                sb.Append($" DestinationRect=\"{ToRect(destinationRect)}\"");
            }

            sb.Append($">{NewLine}");

            if (UseBrushTransform)
            {
                if (pictureShader?.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on VisualBrush
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(pictureShader.LocalMatrix);

                    if (!UseCompatMode)
                    {
                        localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                    }

                    sb.Append($"  <VisualBrush.Transform>{NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}");
                    sb.Append($"  </VisualBrush.Transform>{NewLine}");
                }
            }

            if (pictureShader?.Src is not null)
            {
                sb.Append($"  <VisualBrush.Visual>{NewLine}");
                sb.Append(ToXamlImage(pictureShader.Src));
                sb.Append($"{NewLine}");
                sb.Append($"  </VisualBrush.Visual>{NewLine}");
            }

            sb.Append($"</VisualBrush>{NewLine}");

            return sb.ToString();
        }

        private string ToBrush(ShimSkiaSharp.SKShader skShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            return skShader switch
            {
                ShimSkiaSharp.ColorShader colorShader => ToBrush(colorShader, skBounds, key),
                ShimSkiaSharp.LinearGradientShader linearGradientShader => ToBrush(linearGradientShader, skBounds, key),
                ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader => ToBrush(twoPointConicalGradientShader, skBounds, key),
                ShimSkiaSharp.PictureShader pictureShader => ToBrush(pictureShader, skBounds, key),
                _ => ""
            };
        }

        private string ToPen(ShimSkiaSharp.SKPaint skPaint, SkiaSharp.SKRect skBounds, string? key = null)
        {
            if (skPaint.Shader is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<Pen{ToKey(key)}");

            if (skPaint.Shader is ShimSkiaSharp.ColorShader colorShader)
            {
                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (skPaint.StrokeWidth != 1.0)
            {
                sb.Append($" Thickness=\"{ToString(skPaint.StrokeWidth)}\"");
            }

            if (skPaint.StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
            {
                if (UseCompatMode)
                {
                    sb.Append($" StartLineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"");
                    sb.Append($" EndLineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"");
                }
                else
                {
                    sb.Append($" LineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"");
                }
            }

            if (UseCompatMode)
            {
                if (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } })
                {
                    if (skPaint.StrokeCap != ShimSkiaSharp.SKStrokeCap.Square)
                    {
                        sb.Append($" DashCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"");
                    }
                }

                if (skPaint.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Miter)
                {
                    sb.Append($" LineJoin=\"{ToPenLineJoin(skPaint.StrokeJoin)}\"");
                }
            }
            else
            {
                if (skPaint.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
                {
                    sb.Append($" LineJoin=\"{ToPenLineJoin(skPaint.StrokeJoin)}\"");
                }
            }

            if (UseCompatMode)
            {
                var miterLimit = skPaint.StrokeMiter;
                var strokeWidth = skPaint.StrokeWidth;

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
                    sb.Append($" MiterLimit=\"{ToString(miterLimit)}\"");
                }
            }
            else
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (skPaint.StrokeMiter != 10.0)
                {
                    sb.Append($" MiterLimit=\"{ToString(skPaint.StrokeMiter)}\"");
                }
            }

            if (skPaint.Shader is not ShimSkiaSharp.ColorShader || (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } }))
            {
                sb.Append($">{NewLine}");
            }
            else
            {
                sb.Append($"/>{NewLine}");
            }

            if (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect dashPathEffect && dashPathEffect.Intervals is { })
            {
                var dashes = new List<double>();

                foreach (var interval in dashPathEffect.Intervals)
                {
                    dashes.Add(interval / skPaint.StrokeWidth);
                }

                var offset = dashPathEffect.Phase / skPaint.StrokeWidth;

                sb.Append($"  <Pen.DashStyle>{NewLine}");
                sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(ToString))}\" Offset=\"{ToString(offset)}\"/>{NewLine}");
                sb.Append($"  </Pen.DashStyle>{NewLine}");
            }

            if (skPaint.Shader is not ShimSkiaSharp.ColorShader)
            {
                sb.Append($"  <Pen.Brush>{NewLine}");
                sb.Append(ToBrush(skPaint.Shader, skBounds));
                sb.Append($"  </Pen.Brush>{NewLine}");
            }

            if (skPaint.Shader is not ShimSkiaSharp.ColorShader || (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } }))
            {
                sb.Append($"</Pen>{NewLine}");
            }

            return sb.ToString();
        }

        private void ToXamlGeometryDrawing(SkiaSharp.SKPath path, ShimSkiaSharp.SKPaint skPaint, StringBuilder sb, Resources? resources = null, bool reuseExistingResources = false)
        {
            sb.Append($"<GeometryDrawing");

            var isFilled = skPaint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Fill;
            var isStroked = skPaint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Stroke;

            if (isFilled && skPaint.Shader is ShimSkiaSharp.ColorShader colorShader && resources is null)
            {
                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
            }

            var brush = default(string);
            var pen = default(string);

            if (isFilled && skPaint.Shader is { } && resources is null && skPaint.Shader is not ShimSkiaSharp.ColorShader)
            {
                brush = ToBrush(skPaint.Shader, path.Bounds);
            }

            if (isFilled && skPaint.Shader is { } && resources is { })
            {
                bool haveBrush = false;

                if (reuseExistingResources)
                {
                    var existingBrush = resources.Brushes.FirstOrDefault(x =>
                    {
                        if (x.Value.Paint.Shader is { } 
                            && x.Value.Paint.Shader.Equals(skPaint.Shader))
                        {
                            return true;
                        }

                        return false;
                    });

                    if (!string.IsNullOrEmpty(existingBrush.Key))
                    {
                        sb.Append($" Brush=\"{{DynamicResource {existingBrush.Key}}}\"");
                        haveBrush = true;
                    }
                }

                if (!haveBrush)
                {
                    var resourceKey = $"Brush{resources.BrushCounter++}";
                    var brushResource = ToBrush(skPaint.Shader, path.Bounds, resourceKey);
                    if (!string.IsNullOrEmpty(brushResource))
                    {
                        sb.Append($" Brush=\"{{DynamicResource {resourceKey}}}\"");
                        resources.Brushes.Add(resourceKey, (skPaint, brushResource));
                    }
                }
            }

            if (isStroked && skPaint.Shader is { } && resources is null)
            {
                pen = ToPen(skPaint, path.Bounds);
            }

            if (isStroked && skPaint.Shader is { } && resources is { })
            {
                bool havePen = false;

                if (reuseExistingResources)
                {
                    var existingPen = resources.Pens.FirstOrDefault(x =>
                    {
                        if (x.Value.Paint.Shader is { } 
                            && x.Value.Paint.Shader.Equals(skPaint.Shader)
                            && x.Value.Paint.StrokeWidth.Equals(skPaint.StrokeWidth)
                            && x.Value.Paint.StrokeCap.Equals(skPaint.StrokeCap)
                            && x.Value.Paint.PathEffect == skPaint.PathEffect
                            && x.Value.Paint.StrokeJoin.Equals(skPaint.StrokeJoin)
                            && x.Value.Paint.StrokeMiter.Equals(skPaint.StrokeMiter))
                        {
                            return true;
                        }

                        return false;
                    });

                    if (!string.IsNullOrEmpty(existingPen.Key))
                    {
                        sb.Append($" Pen=\"{{DynamicResource {existingPen.Key}}}\"");
                        havePen = true;
                    }
                }

                if (!havePen)
                {
                    var resourceKey = $"Pen{resources.PenCounter++}";
                    var penResource = ToPen(skPaint, path.Bounds, resourceKey);
                    if (!string.IsNullOrEmpty(penResource))
                    {
                        sb.Append($" Pen=\"{{DynamicResource {resourceKey}}}\"");

                        resources.Pens.Add(resourceKey, (skPaint, penResource));
                    }
                }
            }

            sb.Append($" Geometry=\"{ToSvgPathData(path)}\"");

            if (!string.IsNullOrEmpty(brush) || !string.IsNullOrEmpty(pen))
            {
                sb.Append($">{NewLine}");
            }
            else
            {
                sb.Append($"/>{NewLine}");
            }

            if (!string.IsNullOrEmpty(brush))
            {
                sb.Append($"  <GeometryDrawing.Brush>{NewLine}");
                sb.Append($"{brush}");
                sb.Append($"  </GeometryDrawing.Brush>{NewLine}");
            }

            if (!string.IsNullOrEmpty(pen))
            {
                sb.Append($"  <GeometryDrawing.Pen>{NewLine}");
                sb.Append($"{pen}");
                sb.Append($"  </GeometryDrawing.Pen>{NewLine}");
            }

            if (!string.IsNullOrEmpty(brush) || !string.IsNullOrEmpty(pen))
            {
                sb.Append($"</GeometryDrawing>{NewLine}");
            }
        }

        public string ToXamlDrawingGroup(ShimSkiaSharp.SKPicture? skPicture, Resources? resources = null, bool reuseExistingResources = false, string? key = null)
        {
            if (skPicture?.Commands is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup{ToKey(key)}>{NewLine}");

            var totalMatrixStack = new Stack<SkiaSharp.SKMatrix?>();
            var currentTotalMatrix = default(SkiaSharp.SKMatrix?);

            var clipPathStack = new Stack<SkiaSharp.SKPath?>();
            var currentClipPath = default(SkiaSharp.SKPath?);

            var layersStack = new Stack<(StringBuilder Builder, LayerType Type, object? Value)?>();

            foreach (var canvasCommand in skPicture.Commands)
            {
                switch (canvasCommand)
                {
                    case ShimSkiaSharp.ClipPathCanvasCommand(var clipPath, _, _):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                        if (path is null)
                        {
                            break;
                        }

                        var clipGeometry = ToSvgPathData(path);

                        Debug($"StartClipPath({clipPathStack.Count})");

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");

                        currentClipPath = path;

                        break;
                    }
                    case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                    {
                        var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                        var path = new SkiaSharp.SKPath();
                        path.AddRect(rect);

                        var clipGeometry = ToSvgPathData(path);

                        Debug($"StarClipPath({clipPathStack.Count})");

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");

                        currentClipPath = path;

                        break;
                    }
                    case ShimSkiaSharp.SetMatrixCanvasCommand(var skMatrix):
                    {
                        var matrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(skMatrix);
                        if (matrix.IsIdentity)
                        {
                            break;
                        }

                        var previousMatrixList = new List<SkiaSharp.SKMatrix>();

                        foreach (var totalMatrixList in totalMatrixStack)
                        {
                            if (totalMatrixList is { } totalMatrix)
                            {
                                previousMatrixList.Add(totalMatrix);
                            }
                        }

                        previousMatrixList.Reverse();

                        foreach (var previousMatrix in previousMatrixList)
                        {
                            var inverted = previousMatrix.Invert();
                            matrix = inverted.PreConcat(matrix);
                        }

                        Debug($"StarMatrix({totalMatrixStack.Count})");

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.Transform>{NewLine}");
                        sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{NewLine}");
                        sb.Append($"  </DrawingGroup.Transform>{NewLine}");

                        currentTotalMatrix = matrix;

                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(var count, var skPaint):
                    {
                        // Mask

                        if (skPaint is null)
                        {
                            break;
                        }

                        var isMaskBrush = skPaint.Shader is null 
                                          && skPaint.ColorFilter is { }
                                          && skPaint.ImageFilter is null 
                                          && skPaint.Color is { } skMaskEndColor 
                                          && skMaskEndColor.Equals(s_transparentBlack);
                        if (isMaskBrush)
                        {
                            SaveLayer(LayerType.MaskBrush, skPaint, count);

                            break;
                        }

                        var isMaskGroup = skPaint.Shader is null 
                                          && skPaint.ColorFilter is null 
                                          && skPaint.ImageFilter is null 
                                          && skPaint.Color is { } skMaskStartColor 
                                          && skMaskStartColor.Equals(s_transparentBlack);
                        if (isMaskGroup)
                        {
                            SaveLayer(LayerType.MaskGroup, skPaint, count);

                            break;
                        }

                        // Opacity

                        var isOpacityGroup = skPaint.Shader is null 
                                             && skPaint.ColorFilter is null 
                                             && skPaint.ImageFilter is null 
                                             && skPaint.Color is { Alpha: < OpaqueAlpha };
                        if (isOpacityGroup)
                        {
                            SaveLayer(LayerType.OpacityGroup, skPaint, count);

                            break;
                        }

                        // Filter

                        var isFilterGroup = skPaint.Shader is null 
                                            && skPaint.ColorFilter is null
                                            && skPaint.ImageFilter is { } 
                                            && skPaint.Color is { } skFilterColor
                                            && skFilterColor.Equals(s_transparentBlack);
                        if (isFilterGroup)
                        {
                            SaveLayer(LayerType.FilterGroup, skPaint, count);

                            break;
                        }

                        SaveLayer(LayerType.UnknownPaint, skPaint, count);

                        break;
                    }
                    case ShimSkiaSharp.SaveCanvasCommand(var count):
                    {
                        EmptyLayer();
                        Save(count);

                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand(var count):
                    {
                        Restore(count);

                        break;
                    }
                    case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                        if (!path.IsEmpty)
                        {
                            break;
                        }

                        ToXamlGeometryDrawing(path, skPaint, sb, resources, reuseExistingResources);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextCanvasCommand(var text, var x, var y, var skPaint):
                    {
                        var paint = Svg.Skia.SkiaModelExtensions.ToSKPaint(skPaint);
                        var path = paint.GetTextPath(text, x, y);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        Debug($"Text='{text}'");

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Center)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width / 2f, 0f));
                        }

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Right)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width, 0f));
                        }

                        ToXamlGeometryDrawing(path, skPaint, sb, resources, reuseExistingResources);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: TextOnPath");

                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: TextBlob");

                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: Image");

                        break;
                    }
                }
            }

            void Debug(string message)
            {
#if DEBUG
                sb.Append($"<!-- {message} -->{NewLine}");
#endif
            }

            void EmptyLayer()
            {
                layersStack.Push(null);
            }

            void SaveLayer(LayerType type, object? value, int count)
            {
                Debug($"SaveLayer({type}, {count})");

                layersStack.Push((sb, type, value));
                sb = new StringBuilder();

                Save(count);
            }

            void RestoreLayer()
            {
                // layers

                var layer = layersStack.Count > 0 ? layersStack.Pop() : null;
                if (layer is null)
                {
                    return;
                }

                var (builder, type, value) = layer.Value;
                var content = sb.ToString();

                sb = builder;

                Debug($"StartLayer({type})");

                switch (type)
                {
                    case LayerType.UnknownPaint:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append(content);

                        break;
                    }
                    /*
                    case LayerType.ClipPathGroup:
                    {
                        if (value is not SkiaSharp.SKPath path)
                        {
                            break;
                        }

                        var clipGeometry = ToSvgPathData(path);

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");
                        sb.Append(content);
                        sb.Append($"</DrawingGroup>{NewLine}");

                        break;
                    }
                    case LayerType.MatrixGroup:
                    {
                        if (value is not SkiaSharp.SKMatrix matrix)
                        {
                            break;
                        }

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.Transform>{NewLine}");
                        sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{NewLine}");
                        sb.Append($"  </DrawingGroup.Transform>{NewLine}");
                        sb.Append(content);
                        sb.Append($"</DrawingGroup>{NewLine}");

                        break;
                    }
                    */
                    case LayerType.MaskGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append(content);
                        sb.Append($"</DrawingGroup>{NewLine}");

                        break;
                    }
                    case LayerType.MaskBrush:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append($"<DrawingGroup.OpacityMask>{NewLine}");
                        sb.Append($"  <VisualBrush");
                        sb.Append($" TileMode=\"None\"");
                        sb.Append($" Stretch=\"None\"");

                        if (UseCompatMode)
                        {
                            // sb.Append($" Viewport=\"{ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                            // sb.Append($" Viewbox=\"{ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
                        }
                        else
                        {
                            // sb.Append($" SourceRect=\"{ToRect(sourceRect)}\"");
                            // sb.Append($" DestinationRect=\"{ToRect(destinationRect)}\"");
                        }

                        sb.Append($">{NewLine}");
                        sb.Append($"    <VisualBrush.Visual>{NewLine}");
                        sb.Append($"      <Image>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"      <Image.Source>{NewLine}");
                        }

                        sb.Append($"        <DrawingImage>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"        <DrawingImage.Drawing>{NewLine}");
                        }

                        sb.Append($"          <DrawingGroup>{NewLine}");
                        sb.Append(content);
                        sb.Append($"          </DrawingGroup>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"        </DrawingImage.Drawing>{NewLine}");
                        }

                        sb.Append($"        </DrawingImage>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"      </Image.Source>{NewLine}");
                        }

                        sb.Append($"      </Image>{NewLine}");
                        sb.Append($"    </VisualBrush.Visual>{NewLine}");
                        sb.Append($"  </VisualBrush>{NewLine}");
                        sb.Append($"</DrawingGroup.OpacityMask>{NewLine}");

                        break;
                    }
                    case LayerType.OpacityGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint paint)
                        {
                            break;
                        }

                        if (paint.Color is { } skColor)
                        {
                            sb.Append($"<DrawingGroup Opacity=\"{ToString(skColor.Alpha / 255.0)}\">{NewLine}");
                            sb.Append(content);
                            sb.Append($"</DrawingGroup>{NewLine}");
                        }

                        break;
                    }
                    case LayerType.FilterGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append(content);

                        break;
                    }
                }

                Debug($"EndLayer({type})");
            }

            void SaveGroups()
            {
                // matrix

                totalMatrixStack.Push(currentTotalMatrix);
                currentTotalMatrix = default;

                // clip-path

                clipPathStack.Push(currentClipPath);
                currentClipPath = default;
            }

            void RestoreGroups()
            {
                // clip-path

                if (currentClipPath is { })
                {
                    sb.Append($"</DrawingGroup>{NewLine}");

                    Debug($"EndClipPath({clipPathStack.Count})");
                }

                currentClipPath = default;

                if (clipPathStack.Count > 0)
                {
                    currentClipPath = clipPathStack.Pop();
                }

                // matrix

                if (currentTotalMatrix is { })
                {
                    sb.Append($"</DrawingGroup>{NewLine}");

                    Debug($"EndMatrix({totalMatrixStack.Count})");
                }

                currentTotalMatrix = default;

                if (totalMatrixStack.Count > 0)
                {
                    currentTotalMatrix = totalMatrixStack.Pop();
                }
            }

            void Save(int count)
            {
                Debug($"Save({count})");
                SaveGroups();
            }

            void Restore(int count)
            {
                Debug($"Restore({count})");
                RestoreLayer();
                RestoreGroups();
            }

            RestoreGroups();

            sb.Append($"</DrawingGroup>");

            return sb.ToString();
        }

        private string ToResources(Resources resources)
        {
            var sb = new StringBuilder();

            foreach (var resource in resources.Brushes)
            {
                sb.Append(resource.Value.Resource);
            }

            foreach (var resource in resources.Pens)
            {
                sb.Append(resource.Value.Resource);
            }

            return sb.ToString();
        }

        public string ToXamlImage(ShimSkiaSharp.SKPicture? skPicture, Resources? resources = null, bool reuseExistingResources = false, string? key = null, bool writeResources = true)
        {
            var sb = new StringBuilder();

            var drawingGroup= ToXamlDrawingGroup(skPicture, resources, reuseExistingResources);

            if (resources is { } && (resources.Brushes.Count > 0 || resources.Pens.Count > 0) && writeResources)
            {
                sb.Append($"<Image{ToKey(key)}");
                // sb.Append(UseCompatMode
                //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                //     : $" xmlns=\"https://github.com/avaloniaui\"");
                // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
                sb.Append($">{NewLine}");
            }
            else
            {
                sb.Append($"<Image{ToKey(key)}");
                sb.Append($">{NewLine}");
            }

            if (resources is { } && (resources.Brushes.Count > 0 || resources.Pens.Count > 0) && writeResources)
            {
                sb.Append($"<Image.Resources>{NewLine}");
                sb.Append(ToResources(resources));
                sb.Append($"</Image.Resources>{NewLine}");
            }

            if (UseCompatMode)
            {
                sb.Append($"<Image.Source>{NewLine}");
            }

            sb.Append($"  <DrawingImage>{NewLine}");

            if (UseCompatMode)
            {
                sb.Append($"  <DrawingImage.Drawing>{NewLine}");
            }

            sb.Append(drawingGroup);

            if (UseCompatMode)
            {
                sb.Append($"  </DrawingImage.Drawing>{NewLine}");
            }

            sb.Append($"  </DrawingImage>{NewLine}");

            if (UseCompatMode)
            {
                sb.Append($"</Image.Source>{NewLine}");
            }

            sb.Append($"</Image>");

            return sb.ToString();
        }

        public string ToXamlStyles(List<string> paths, Resources? resources = null, bool reuseExistingResources = false, bool generateImage = false, bool generatePreview = true)
        {
            var results = new List<(string Path, string Key, string Xaml)>();

            foreach (var path in paths)
            {
                try
                {
                    var svg = new Svg.Skia.SKSvg();
                    svg.Load(path);
                    if (svg.Model is null)
                    {
                        continue;
                    }

                    var key = $"_{CreateKey(path)}";
                    if (generateImage)
                    {
                        var xaml = ToXamlImage(svg.Model, resources, reuseExistingResources, key, writeResources: false);
                        results.Add((path, key, xaml));
                    }
                    else
                    {
                        var xaml = ToXamlDrawingGroup(svg.Model, resources, reuseExistingResources, key);
                        results.Add((path, key, xaml));
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var sb = new StringBuilder();

            if (UseCompatMode)
            {
                sb.Append($"<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"{NewLine}");
                sb.Append($"                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{NewLine}");
            }
            else
            {
                sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{NewLine}");
                sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{NewLine}");
            }

            if (generatePreview && !UseCompatMode)
            {
                sb.Append($"  <Design.PreviewWith>");
                sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">");
                sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">");

                foreach (var result in results)
                {
                    if (generateImage)
                    {
                        sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>");
                    }
                    else
                    {
                        sb.Append($"        <Image>");

                        if (UseCompatMode)
                        {
                            sb.Append($"            <Image.Source>");
                        }

                        sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>");

                        if (UseCompatMode)
                        {
                            sb.Append($"            </Image.Source>");
                        }

                        sb.Append($"        </Image>");
                    }
                }

                sb.Append($"      </WrapPanel>");
                sb.Append($"    </ScrollViewer>");
                sb.Append($"  </Design.PreviewWith>");
            }

            if (!UseCompatMode)
            {
                sb.Append($"  <Style>{NewLine}");
                sb.Append($"    <Style.Resources>{NewLine}");
            }

            if (resources is { } && (resources.Brushes.Count > 0 || resources.Pens.Count > 0))
            {
                sb.Append(ToResources(resources));
            }

            foreach (var result in results)
            {
                sb.Append($"<!-- {Path.GetFileName(result.Path)} -->{NewLine}");
                sb.Append(result.Xaml);
                sb.Append(NewLine);
            }

            if (UseCompatMode)
            {
                sb.Append($"</ResourceDictionary>");
            }
            else
            {
                sb.Append($"    </Style.Resources>{NewLine}");
                sb.Append($"  </Style>{NewLine}");
                sb.Append($"</Styles>");
            }

            return sb.ToString();
        }

        public virtual string CreateKey(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string key = name.Replace("-", "_");
            return $"_{key}";
        }

        public string Format(string xml)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append($"<Root");
                sb.Append(UseCompatMode
                    ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                    : $" xmlns=\"https://github.com/avaloniaui\"");
                sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
                sb.Append($">");
                sb.Append(xml);
                sb.Append($"</Root>");

                using var ms = new MemoryStream();
                using var writer = new XmlTextWriter(ms, Encoding.UTF8);
                var document = new XmlDocument();
                document.LoadXml(sb.ToString());
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                document.WriteContentTo(writer);
                writer.Flush();
                ms.Flush();
                ms.Position = 0;
                using var sReader = new StreamReader(ms);
                var formatted = sReader.ReadToEnd();

                var lines = formatted.Split(NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var inner = lines.Skip(1).Take(lines.Length - 2).Select(x => x.Substring(2, x.Length - 2));
                return string.Join(NewLine, inner);
            }
            catch
            {
                // ignored
            }

            return "";
        }
    }
}
