using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SvgToXamlConverter.Model.Containers;
using SvgToXamlConverter.Model.Drawing;
using SvgToXamlConverter.Model.Paint;
using SvgToXamlConverter.Model.Resources;

namespace SvgToXamlConverter.Generator
{
    public class XamlGenerator : GeneratorBase
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

        private static string ToSvgPathData(SkiaSharp.SKPath path)
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

        public override string Generate(Brush brush, GeneratorContext context)
        {
            return brush switch
            {
                SolidColorBrush solidColorBrush => Generate(solidColorBrush, context),
                LinearGradientBrush linearGradientBrush => Generate(linearGradientBrush, context),
                RadialGradientBrush radialGradientBrush => Generate(radialGradientBrush, context),
                TwoPointConicalGradientBrush twoPointConicalGradientBrush => Generate(twoPointConicalGradientBrush, context),
                PictureBrush pictureBrush => Generate(pictureBrush, context),
                _ => ""
            };
        }

        public override string Generate(SolidColorBrush solidColorBrush, GeneratorContext context)
        {
            var sb = new StringBuilder();

            sb.Append($"<SolidColorBrush{ToKey(solidColorBrush.Key)}");
            sb.Append($" Color=\"{ToHexColor(solidColorBrush.Color)}\"");
            sb.Append($"/>{context.NewLine}");

            return sb.ToString();
        }

        public override string Generate(LinearGradientBrush linearGradientBrush, GeneratorContext context)
        {
            var sb = new StringBuilder();

            var start = linearGradientBrush.Start;
            var end = linearGradientBrush.End;

            if (!context.UseBrushTransform)
            {
                if (linearGradientBrush.LocalMatrix is { })
                {
                    var localMatrix = linearGradientBrush.LocalMatrix.Value;
                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - linearGradientBrush.Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - linearGradientBrush.Bounds.Location.Y);

                    start = localMatrix.MapPoint(start);
                    end = localMatrix.MapPoint(end);
                }
                else
                {
                    start.X = Math.Max(0f, start.X - linearGradientBrush.Bounds.Location.X);
                    start.Y = Math.Max(0f, start.Y - linearGradientBrush.Bounds.Location.Y);
                    end.X = Math.Max(0f, end.X - linearGradientBrush.Bounds.Location.X);
                    end.Y = Math.Max(0f, end.Y - linearGradientBrush.Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (linearGradientBrush.LocalMatrix is null)
                    {
                        start.X = Math.Max(0f, start.X - linearGradientBrush.Bounds.Location.X);
                        start.Y = Math.Max(0f, start.Y - linearGradientBrush.Bounds.Location.Y);
                        end.X = Math.Max(0f, end.X - linearGradientBrush.Bounds.Location.X);
                        end.Y = Math.Max(0f, end.Y - linearGradientBrush.Bounds.Location.Y);
                    }
                }
            }

            sb.Append($"<LinearGradientBrush{ToKey(linearGradientBrush.Key)}");

            sb.Append($" StartPoint=\"{ToPoint(start)}\"");
            sb.Append($" EndPoint=\"{ToPoint(end)}\"");

            if (linearGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientBrush.Mode)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (linearGradientBrush.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on LinearGradientBrush
                    var localMatrix = linearGradientBrush.LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = linearGradientBrush.WithTransXY(localMatrix, linearGradientBrush.Bounds.Location.X, linearGradientBrush.Bounds.Location.Y);
                    }

                    sb.Append($"  <LinearGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </LinearGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (linearGradientBrush.GradientStops.Count > 0)
            {
                sb.Append($"  <LinearGradientBrush.GradientStops>{context.NewLine}");

                foreach (var stop in linearGradientBrush.GradientStops)
                {
                    var color = ToHexColor(stop.Color);
                    var offset = ToXamlString(stop.Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </LinearGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</LinearGradientBrush>{context.NewLine}");

            return sb.ToString();
        }

        public override string Generate(RadialGradientBrush radialGradientBrush, GeneratorContext context)
        {
           var sb = new StringBuilder();

            var radius = radialGradientBrush.Radius;

            var center = radialGradientBrush.Center;
            var gradientOrigin = radialGradientBrush.Center;

            if (!context.UseBrushTransform)
            {
                if (radialGradientBrush.LocalMatrix is { })
                {
                    var localMatrix = radialGradientBrush.LocalMatrix.Value;

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - radialGradientBrush.Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - radialGradientBrush.Bounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radiusMapped = localMatrix.MapVector(new SkiaSharp.SKPoint(radius, 0));
                    radius = radiusMapped.X;
                }
                else
                {
                    center.X = Math.Max(0f, center.X - radialGradientBrush.Bounds.Location.X);
                    center.Y = Math.Max(0f, center.Y - radialGradientBrush.Bounds.Location.Y);
                    gradientOrigin.X = Math.Max(0f, gradientOrigin.X - radialGradientBrush.Bounds.Location.X);
                    gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - radialGradientBrush.Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (radialGradientBrush.LocalMatrix is null)
                    {
                        center.X = Math.Max(0f, center.X - radialGradientBrush.Bounds.Location.X);
                        center.Y = Math.Max(0f, center.Y - radialGradientBrush.Bounds.Location.Y);
                        gradientOrigin.X = Math.Max(0f, gradientOrigin.X - radialGradientBrush.Bounds.Location.X);
                        gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - radialGradientBrush.Bounds.Location.Y);
                    }
                }
            }

            if (!context.UseCompatMode)
            {
                radius = radius / radialGradientBrush.Bounds.Width;
            }

            sb.Append($"<RadialGradientBrush{ToKey(radialGradientBrush.Key)}");

            sb.Append($" Center=\"{ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{ToPoint(gradientOrigin)}\"");

            if (context.UseCompatMode)
            {
                sb.Append($" RadiusX=\"{ToXamlString(radius)}\"");
                sb.Append($" RadiusY=\"{ToXamlString(radius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{ToXamlString(radius)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            if (radialGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(radialGradientBrush.Mode)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (radialGradientBrush.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on RadialGradientBrush
                    var localMatrix = radialGradientBrush.LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = radialGradientBrush.WithTransXY(localMatrix, radialGradientBrush.Bounds.Location.X, radialGradientBrush.Bounds.Location.Y);
                    }

                    sb.Append($"  <RadialGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </RadialGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (radialGradientBrush.GradientStops.Count > 0)
            {
                sb.Append($"  <RadialGradientBrush.GradientStops>{context.NewLine}");

                foreach (var stop in radialGradientBrush.GradientStops)
                {
                    var color = ToHexColor(stop.Color);
                    var offset = ToXamlString(stop.Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </RadialGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</RadialGradientBrush>{context.NewLine}");

            return sb.ToString();
        }

        public override string Generate(TwoPointConicalGradientBrush twoPointConicalGradientBrush, GeneratorContext context)
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

            if (!context.UseBrushTransform)
            {
                if (twoPointConicalGradientBrush.LocalMatrix is { })
                {
                    var localMatrix = twoPointConicalGradientBrush.LocalMatrix.Value;

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - twoPointConicalGradientBrush.Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - twoPointConicalGradientBrush.Bounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radiusMapped = localMatrix.MapVector(new SkiaSharp.SKPoint(endRadius, 0));
                    endRadius = radiusMapped.X;
                }
                else
                {
                    center.X = Math.Max(0f, center.X - twoPointConicalGradientBrush.Bounds.Location.X);
                    center.Y = Math.Max(0f, center.Y - twoPointConicalGradientBrush.Bounds.Location.Y);
                    gradientOrigin.X = Math.Max(0f, gradientOrigin.X - twoPointConicalGradientBrush.Bounds.Location.X);
                    gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - twoPointConicalGradientBrush.Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (twoPointConicalGradientBrush.LocalMatrix is null)
                    {
                        center.X = Math.Max(0f, center.X - twoPointConicalGradientBrush.Bounds.Location.X);
                        center.Y = Math.Max(0f, center.Y - twoPointConicalGradientBrush.Bounds.Location.Y);
                        gradientOrigin.X = Math.Max(0f, gradientOrigin.X - twoPointConicalGradientBrush.Bounds.Location.X);
                        gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - twoPointConicalGradientBrush.Bounds.Location.Y);
                    }
                }
            }

            if (!context.UseCompatMode)
            {
                endRadius = endRadius / twoPointConicalGradientBrush.Bounds.Width;
            }

            sb.Append($"<RadialGradientBrush{ToKey(twoPointConicalGradientBrush.Key)}");

            sb.Append($" Center=\"{ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{ToPoint(gradientOrigin)}\"");

            if (context.UseCompatMode)
            {
                sb.Append($" RadiusX=\"{ToXamlString(endRadius)}\"");
                sb.Append($" RadiusY=\"{ToXamlString(endRadius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{ToXamlString(endRadius)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            if (twoPointConicalGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(twoPointConicalGradientBrush.Mode)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (twoPointConicalGradientBrush.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on RadialGradientBrush
                    var localMatrix = twoPointConicalGradientBrush.LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = twoPointConicalGradientBrush.WithTransXY(localMatrix, twoPointConicalGradientBrush.Bounds.Location.X, twoPointConicalGradientBrush.Bounds.Location.Y);
                    }

                    sb.Append($"  <RadialGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </RadialGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (twoPointConicalGradientBrush.GradientStops.Count > 0)
            {
                sb.Append($"  <RadialGradientBrush.GradientStops>{context.NewLine}");

                foreach (var stop in twoPointConicalGradientBrush.GradientStops)
                {
                    var color = ToHexColor(stop.Color);
                    var offset = ToXamlString(stop.Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </RadialGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</RadialGradientBrush>{context.NewLine}");

            return sb.ToString();
        }

        public override string Generate(PictureBrush pictureBrush, GeneratorContext context)
        {
            if (pictureBrush.Picture is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            if (!context.UseBrushTransform)
            {
                if (pictureBrush.LocalMatrix is { })
                {
                    var localMatrix = pictureBrush.LocalMatrix.Value;

                    if (!localMatrix.IsIdentity)
                    {
                        // TODO: LocalMatrix
                    }
                }
                else
                {
                    // TODO: Adjust using Bounds.Location ?
                }
            }

            var sourceRect = pictureBrush.CullRect;
            var destinationRect = pictureBrush.Tile;

            // TODO: Use different visual then Image ?
            sb.Append($"<VisualBrush{ToKey(pictureBrush.Key)}");

            if (pictureBrush.TileMode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" TileMode=\"{ToTileMode(pictureBrush.TileMode)}\"");
            }

            if (context.UseCompatMode)
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

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (pictureBrush.LocalMatrix is { })
                {
                    // TODO: Missing Transform property on VisualBrush
                    var localMatrix = pictureBrush.LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = pictureBrush.WithTransXY(localMatrix, pictureBrush.Bounds.Location.X, pictureBrush.Bounds.Location.Y);
                    }

                    sb.Append($"  <VisualBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </VisualBrush.Transform>{context.NewLine}");
                }
            }

            if (pictureBrush.Picture is not null)
            {
                sb.Append($"  <VisualBrush.Visual>{context.NewLine}");
                sb.Append(Generate(pictureBrush.Picture, context with { WriteResources = false }));
                sb.Append($"{context.NewLine}");
                sb.Append($"  </VisualBrush.Visual>{context.NewLine}");
            }

            sb.Append($"</VisualBrush>{context.NewLine}");

            return sb.ToString();
        }

        public override string Generate(Pen pen, GeneratorContext context)
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
                if (context.UseCompatMode)
                {
                    sb.Append($" StartLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                    sb.Append($" EndLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                }
                else
                {
                    sb.Append($" LineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                }
            }

            if (context.UseCompatMode)
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

            if (context.UseCompatMode)
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
                sb.Append($">{context.NewLine}");
            }
            else
            {
                sb.Append($"/>{context.NewLine}");
            }

            if (pen.Dashes is { Intervals: { } })
            {
                var dashes = new List<double>();

                foreach (var interval in pen.Dashes.Intervals)
                {
                    dashes.Add(interval / pen.StrokeWidth);
                }

                var offset = pen.Dashes.Phase / pen.StrokeWidth;

                sb.Append($"  <Pen.DashStyle>{context.NewLine}");
                sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(ToXamlString))}\" Offset=\"{ToXamlString(offset)}\"/>{context.NewLine}");
                sb.Append($"  </Pen.DashStyle>{context.NewLine}");
            }

            if (pen.Brush is not SolidColorBrush)
            {
                sb.Append($"  <Pen.Brush>{context.NewLine}");
                sb.Append(Generate(pen.Brush, context));
                sb.Append($"  </Pen.Brush>{context.NewLine}");
            }

            if (pen.Brush is not SolidColorBrush || pen.Dashes is { Intervals: { } })
            {
                sb.Append($"</Pen>{context.NewLine}");
            }

            return sb.ToString();
        }

        public override string Generate(Drawing drawing, GeneratorContext context)
        {
            return drawing switch
            {
                GeometryDrawing geometryDrawing => Generate(geometryDrawing, context),
                DrawingGroup drawingGroup => Generate(drawingGroup, context),
                DrawingImage drawingImage => Generate(drawingImage, context),
                _ => ""
            };
        }

        public override string Generate(GeometryDrawing geometryDrawing, GeneratorContext context)
        {
            if (geometryDrawing.Paint is null || geometryDrawing.Geometry is null)
            {
                return "";
            }
 
            var sb = new StringBuilder();
            
            sb.Append($"<GeometryDrawing");

            var isFilled = geometryDrawing.Brush is { };
            var isStroked = geometryDrawing.Pen is { };

            if (isFilled && geometryDrawing.Brush is SolidColorBrush solidColorBrush && context.Resources is null)
            {
                sb.Append($" Brush=\"{ToHexColor(solidColorBrush.Color)}\"");
            }

            var brush = default(Brush);
            var pen = default(Pen);

            if (isFilled && geometryDrawing.Brush is { } and not SolidColorBrush && context.Resources is null)
            {
                brush =geometryDrawing. Brush;
            }

            if (isFilled && geometryDrawing.Paint is { } && context.Resources is { })
            {
                bool haveBrush = false;

                if (context.ReuseExistingResources)
                {
                    var existingBrush = context.Resources.Brushes.FirstOrDefault(x =>
                    {
                        if (x.Value.Paint.Shader is { } && x.Value.Paint.Shader.Equals(geometryDrawing.Paint.Shader))
                        {
                            return true;
                        }

                        return false;
                    });

                    if (!string.IsNullOrEmpty(existingBrush.Key))
                    {
                        context.Resources.UseBrushes.Add(existingBrush.Value.Brush);
                        sb.Append($" Brush=\"{{DynamicResource {existingBrush.Key}}}\"");
                        haveBrush = true;
                    }
                }

                if (!haveBrush)
                {
                    if (geometryDrawing.Brush is { } && context.Resources is { } && geometryDrawing.Brush.Key is { })
                    {
                        context.Resources.UseBrushes.Add(geometryDrawing.Brush);
                        sb.Append($" Brush=\"{{DynamicResource {geometryDrawing.Brush.Key}}}\"");
                        haveBrush = true;
                    }
                    
                    if (!haveBrush)
                    {
                        brush = geometryDrawing.Brush;
                    }
                }
            }

            if (isStroked && geometryDrawing.Pen is { } && context.Resources is null)
            {
                pen = geometryDrawing.Pen;
            }

            if (isStroked && geometryDrawing.Paint is { } && context.Resources is { })
            {
                bool havePen = false;

                if (context.ReuseExistingResources)
                {
                    var existingPen = context.Resources.Pens.FirstOrDefault(x =>
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
                        context.Resources.UsePens.Add(existingPen.Value.Pen);
                        sb.Append($" Pen=\"{{DynamicResource {existingPen.Key}}}\"");
                        havePen = true;
                    }
                }

                if (!havePen)
                {
                    if (geometryDrawing.Pen is { } && context.Resources is { } && geometryDrawing.Pen.Key is { })
                    {
                        context.Resources.UsePens.Add(geometryDrawing.Pen);
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
                sb.Append($" Geometry=\"{ToSvgPathData(geometryDrawing.Geometry)}\"");
            }

            if (brush is { } || pen is { })
            {
                sb.Append($">{context.NewLine}");
            }
            else
            {
                sb.Append($"/>{context.NewLine}");
            }

            if (brush is { })
            {
                sb.Append($"  <GeometryDrawing.Brush>{context.NewLine}");
                sb.Append(Generate(brush, context));
                sb.Append($"  </GeometryDrawing.Brush>{context.NewLine}");
            }

            if (pen is { })
            {
                sb.Append($"  <GeometryDrawing.Pen>{context.NewLine}");
                sb.Append(Generate(pen, context));
                sb.Append($"  </GeometryDrawing.Pen>{context.NewLine}");
            }

            if (brush is { } || pen is { })
            {
                sb.Append($"</GeometryDrawing>{context.NewLine}");
            }

            return sb.ToString();
        }

        public override string Generate(DrawingGroup drawingGroup, GeneratorContext context)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup{ToKey(drawingGroup.Key)}");

            if (drawingGroup.Opacity is { })
            {
                sb.Append($" Opacity=\"{ToXamlString(drawingGroup.Opacity.Value)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (drawingGroup.ClipGeometry is { })
            {
                var clipGeometry = ToSvgPathData(drawingGroup.ClipGeometry);

                sb.Append($"  <DrawingGroup.ClipGeometry>{context.NewLine}");
                sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{context.NewLine}");
                sb.Append($"  </DrawingGroup.ClipGeometry>{context.NewLine}");
            }

            if (drawingGroup.Transform is { })
            {
                var matrix = drawingGroup.Transform.Value;

                sb.Append($"  <DrawingGroup.Transform>{context.NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{context.NewLine}");
                sb.Append($"  </DrawingGroup.Transform>{context.NewLine}");
            }

            if (drawingGroup.OpacityMask is { })
            {
                sb.Append($"<DrawingGroup.OpacityMask>{context.NewLine}");
                sb.Append(Generate(drawingGroup.OpacityMask, context));
                sb.Append($"</DrawingGroup.OpacityMask>{context.NewLine}");
            }

            foreach (var child in drawingGroup.Children)
            {
                sb.Append(Generate(child, context));
            }

            sb.Append($"</DrawingGroup>");

            return sb.ToString();
        }

        public override string Generate(DrawingImage drawingImage, GeneratorContext context)
        {
            if (drawingImage.Drawing is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<DrawingImage>{context.NewLine}");

            if (context.UseCompatMode)
            {
                sb.Append($"  <DrawingImage.Drawing>{context.NewLine}");
            }

            sb.Append(Generate(drawingImage.Drawing, context));

            if (context.UseCompatMode)
            {
                sb.Append($"  </DrawingImage.Drawing>{context.NewLine}");
            }

            sb.Append($"</DrawingImage>{context.NewLine}");

            return sb.ToString();
        }

        public override string Generate(Resource resource, GeneratorContext context)
        {
            return resource switch
            {
                SolidColorBrush solidColorBrush => Generate(solidColorBrush, context),
                LinearGradientBrush linearGradientBrush => Generate(linearGradientBrush, context),
                RadialGradientBrush radialGradientBrush => Generate(radialGradientBrush, context),
                TwoPointConicalGradientBrush twoPointConicalGradientBrush => Generate(twoPointConicalGradientBrush, context),
                PictureBrush pictureBrush => Generate(pictureBrush, context),
                Pen pen => Generate(pen, context),
                GeometryDrawing geometryDrawing => Generate(geometryDrawing, context),
                DrawingGroup drawingGroup => Generate(drawingGroup, context),
                DrawingImage drawingImage => Generate(drawingImage, context),
                Image image => Generate(image, context),
                _ => ""
            };
        }

        public override string Generate(ResourceDictionary resourceDictionary, GeneratorContext context)
        {
            var sb = new StringBuilder();

            if (context.ReuseExistingResources)
            {
                foreach (var resource in resourceDictionary.UseBrushes)
                {
                    sb.Append(Generate(resource, context));
                }

                foreach (var resource in resourceDictionary.UsePens)
                {
                    sb.Append(Generate(resource, context));
                }
            }
            else
            {
                foreach (var resource in resourceDictionary.Brushes)
                {
                    sb.Append(Generate(resource.Value.Brush, context));
                }

                foreach (var resource in resourceDictionary.Pens)
                {
                    sb.Append(Generate(resource.Value.Pen, context));
                }               
            }

            return sb.ToString();
        }

        public override string Generate(Image image, GeneratorContext context)
        {
            if (image.Source is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            var content = Generate(image.Source, context);

            sb.Append($"<Image{ToKey(image.Key)}");

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0) && context.WriteResources)
            {
                // sb.Append(context.UseCompatMode
                //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                //     : $" xmlns=\"https://github.com/avaloniaui\"");
                // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0) && context.WriteResources)
            {
                sb.Append($"<Image.Resources>{context.NewLine}");
                sb.Append(Generate(context.Resources, context with { WriteResources = false }));
                sb.Append($"</Image.Resources>{context.NewLine}");
            }

            if (context.UseCompatMode)
            {
                sb.Append($"<Image.Source>{context.NewLine}");
            }

            sb.Append(content);

            if (context.UseCompatMode)
            {
                sb.Append($"</Image.Source>{context.NewLine}");
            }

            sb.Append($"</Image>");

            return sb.ToString();
        }

        public override string Generate(Styles styles, GeneratorContext context)
        {
            if (styles.Resources is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            var content = new StringBuilder();

            foreach (var result in styles.Resources)
            {
                content.Append(Generate(result, context with { WriteResources = false }));
                content.Append(context.NewLine);
            }

            if (context.UseCompatMode)
            {
                sb.Append($"<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"{context.NewLine}");
                sb.Append($"                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{context.NewLine}");
            }
            else
            {
                sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{context.NewLine}");
                sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{context.NewLine}");
            }

            if (styles.GeneratePreview && !context.UseCompatMode)
            {
                sb.Append($"  <Design.PreviewWith>{context.NewLine}");
                sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">{context.NewLine}");
                sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">{context.NewLine}");

                foreach (var result in styles.Resources)
                {
                    if (styles.GenerateImage)
                    {
                        sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>{context.NewLine}");
                    }
                    else
                    {
                        sb.Append($"        <Image>{context.NewLine}");

                        if (context.UseCompatMode)
                        {
                            sb.Append($"            <Image.Source>{context.NewLine}");
                        }

                        sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>{context.NewLine}");

                        if (context.UseCompatMode)
                        {
                            sb.Append($"            </Image.Source>{context.NewLine}");
                        }

                        sb.Append($"        </Image>{context.NewLine}");
                    }
                }

                sb.Append($"      </WrapPanel>{context.NewLine}");
                sb.Append($"    </ScrollViewer>{context.NewLine}");
                sb.Append($"  </Design.PreviewWith>{context.NewLine}");
            }

            if (!context.UseCompatMode)
            {
                sb.Append($"  <Style>{context.NewLine}");
                sb.Append($"    <Style.Resources>{context.NewLine}");
            }

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0))
            {
                sb.Append(Generate(context.Resources, context with { WriteResources = false }));
            }

            sb.Append(content);

            if (context.UseCompatMode)
            {
                sb.Append($"</ResourceDictionary>");
            }
            else
            {
                sb.Append($"    </Style.Resources>{context.NewLine}");
                sb.Append($"  </Style>{context.NewLine}");
                sb.Append($"</Styles>{context.NewLine}");
            }

            return sb.ToString();
        }
    }
}
