using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ShimSkiaSharp;

namespace SvgToXaml
{
    public static class SvgConverter
    {
        public static string ToHexColor(SKColor skColor, string indent = "")
        {
            return $"{indent}#{skColor.Alpha:X2}{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
        }

        public static string ToPoint(SKPoint skPoint)
        {
            return $"{skPoint.X.ToString(CultureInfo.InvariantCulture)},{skPoint.Y.ToString(CultureInfo.InvariantCulture)}";
        }

        public static string ToPoint(SkiaSharp.SKPoint skPoint)
        {
            return $"{skPoint.X.ToString(CultureInfo.InvariantCulture)},{skPoint.Y.ToString(CultureInfo.InvariantCulture)}";
        }

        public static string ToGradientSpreadMethod(SKShaderTileMode shaderTileMode)
        {
            switch (shaderTileMode)
            {
                default:
                case SKShaderTileMode.Clamp:
                    return "Pad";

                case SKShaderTileMode.Repeat:
                    return "Repeat";

                case SKShaderTileMode.Mirror:
                    return "Reflect";
            }
        }

        public static string ToBrush(SKShader skShader, string indent = "")
        {
            if (skShader is ColorShader colorShader)
            {
                var brush = "";

                brush += $"{indent}<SolidColorBrush";
                brush += $" Color=\"{ToHexColor(colorShader.Color)}\"";
                brush += $"/>\r\n";

                return brush;
            }

            if (skShader is LinearGradientShader linearGradientShader)
            {
                var brush = "";

                var start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start);
                var end = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End);

                if (linearGradientShader.LocalMatrix is { })
                {
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);
                    start = localMatrix.MapPoint(start);
                    end = localMatrix.MapPoint(end);
                }

                brush += $"{indent}<LinearGradientBrush";
                brush += $" StartPoint=\"{ToPoint(start)}\"";
                brush += $" EndPoint=\"{ToPoint(end)}\"";
                brush += $" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\">\r\n";
                brush += $"{indent}  <LinearGradientBrush.GradientStops>\r\n";

                if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
                {
                    for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                    {
                        var color = ToHexColor(linearGradientShader.Colors[i]);
                        var offset = linearGradientShader.ColorPos[i].ToString(CultureInfo.InvariantCulture);
                        brush += $"{indent}    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>\r\n";
                    }
                }

                brush += $"{indent}  </LinearGradientBrush.GradientStops>\r\n";
                brush += $"{indent}</LinearGradientBrush>\r\n";

                return brush;
            }

            if (skShader is TwoPointConicalGradientShader twoPointConicalGradientShader)
            {
                // TODO:
            }

            if (skShader is PictureShader pictureShader)
            {
                // TODO:
            }

            return "";
        }

        public static string ToPenLineCap(SKStrokeCap strokeCap)
        {
            switch (strokeCap)
            {
                default:
                case SKStrokeCap.Butt:
                    return "Flat";

                case SKStrokeCap.Round:
                    return "Round";

                case SKStrokeCap.Square:
                    return "Square";
            }
        }

        public static string ToPenLineJoin(SKStrokeJoin strokeJoin)
        {
            switch (strokeJoin)
            {
                default:
                case SKStrokeJoin.Miter:
                    return "Miter";

                case SKStrokeJoin.Round:
                    return "Round";

                case SKStrokeJoin.Bevel:
                    return "Bevel";
            }
        }

        private static string ToPen(SKPaint skPaint, string indent = "")
        {
            if (skPaint.Shader is { })
            {
                var pen = "";

                pen += $"{indent}<Pen";

                if (skPaint.Shader is ColorShader colorShader)
                {
                    pen += $" Brush=\"{ToHexColor(colorShader.Color)}\"";
                }

                if (skPaint.StrokeWidth != 1.0)
                {
                    pen += $" Thickness=\"{skPaint.StrokeWidth.ToString(CultureInfo.InvariantCulture)}\"";
                }

                if (skPaint.StrokeCap != SKStrokeCap.Butt)
                {
                    pen += $" LineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"";
                }

                if (skPaint.StrokeJoin != SKStrokeJoin.Bevel)
                {
                    pen += $" LineJoin=\"{ToPenLineJoin(skPaint.StrokeJoin)}\"";
                }

                if (skPaint.StrokeMiter != 10.0)
                {
                    pen += $" MiterLimit=\"{skPaint.StrokeMiter.ToString(CultureInfo.InvariantCulture)}\"";
                }

                if (skPaint.Shader is not ColorShader || (skPaint.PathEffect is DashPathEffect { Intervals: { } }))
                {
                    pen += $">\r\n";
                }
                else
                {
                    pen += $"/>\r\n";
                }

                if (skPaint.PathEffect is DashPathEffect dashPathEffect && dashPathEffect.Intervals is { })
                {
                    var dashes = new List<double>();

                    foreach (var interval in dashPathEffect.Intervals)
                    {
                        dashes.Add(interval / skPaint.StrokeWidth);
                    }

                    var offset = dashPathEffect.Phase / skPaint.StrokeWidth;

                    pen += $"{indent}  <Pen.DashStyle>\r\n";
                    pen += $"{indent}    <DashStyle Dashes=\"{string.Join(",", dashes.Select(x => x.ToString(CultureInfo.InvariantCulture)))}\" Offset=\"{offset.ToString(CultureInfo.InvariantCulture)}\"/>\r\n";
                    pen += $"{indent}  </Pen.DashStyle>\r\n";
                }

                if (skPaint.Shader is not ColorShader)
                {
                    pen += $"{indent}  <Pen.Brush>\r\n";
                    pen += ToBrush(skPaint.Shader, indent + "    ");
                    pen += $"{indent}  </Pen.Brush>\r\n";
                }

                if (skPaint.Shader is not ColorShader || (skPaint.PathEffect is DashPathEffect { Intervals: { } }))
                {
                    pen += $"{indent}</Pen>\r\n";
                }

                return pen;
            }

            return "";
        }

        public static string ToXaml(SKPicture? skPicture)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup>\r\n");

            if (skPicture?.Commands is { })
            {
                foreach (var canvasCommand in skPicture.Commands)
                {
                    switch (canvasCommand)
                    {
                        case ClipPathCanvasCommand(var clipPath, var skClipOperation, var antialias):
                        {
                            // TODO:
                            break;
                        }
                        case ClipRectCanvasCommand(var skRect, var skClipOperation, var antialias):
                        {
                            // TODO:
                            break;
                        }
                        case SaveCanvasCommand:
                        {
                            // TODO:
                            break;
                        }
                        case RestoreCanvasCommand:
                        {
                            // TODO:
                            break;
                        }
                        case SetMatrixCanvasCommand(var skMatrix):
                        {
                            // TODO:
                            break;
                        }
                        case SaveLayerCanvasCommand(var count, var skPaint):
                        {
                            // TODO:
                            break;
                        }
                        case DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                        {
                            // TODO:
                            break;
                        }
                        case DrawPathCanvasCommand(var skPath, var skPaint):
                        {
                            var indent = "  ";

                            var brush = default(string);
                            var pen = default(string);

                            if ((skPaint.Style == SKPaintStyle.Fill || skPaint.Style == SKPaintStyle.StrokeAndFill) && skPaint.Shader is not ColorShader)
                            {
                                if (skPaint.Shader is { })
                                {
                                    brush = ToBrush(skPaint.Shader, $"{indent}    ");
                                }
                            }

                            if (skPaint.Style == SKPaintStyle.Stroke || skPaint.Style == SKPaintStyle.StrokeAndFill)
                            {
                                if (skPaint.Shader is { })
                                {
                                    pen = ToPen(skPaint, $"{indent}    ");
                                }
                            }

                            sb.Append($"{indent}<GeometryDrawing");

                            if ((skPaint.Style == SKPaintStyle.Fill || skPaint.Style == SKPaintStyle.StrokeAndFill) && skPaint.Shader is ColorShader colorShader)
                            {
                                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
                            }

                            var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                            var data = path.ToSvgPathData();

                            if (skPath.FillType == SKPathFillType.EvenOdd)
                            {
                                // EvenOdd
                                data = $"F0 {data}";
                            }
                            else
                            {
                                // Nonzero 
                                data = $"F1 {data}";
                            }

                            sb.Append($" Geometry=\"{data}\"");

                            if (brush is not null || pen is not null)
                            {
                                sb.Append($">\r\n");
                            }
                            else
                            {
                                sb.Append($"/>\r\n");
                            }

                            if (brush is { })
                            {
                                sb.Append($"{indent}  <GeometryDrawing.Brush>\r\n");
                                sb.Append($"{brush}");
                                sb.Append($"{indent}    </GeometryDrawing.Brush>\r\n");
                            }

                            if (pen is { })
                            {
                                sb.Append($"{indent}  <GeometryDrawing.Pen>\r\n");
                                sb.Append($"{pen}");
                                sb.Append($"{indent}  </GeometryDrawing.Pen>\r\n");
                            }

                            if (brush is not null || pen is not null)
                            {
                                sb.Append($"{indent}</GeometryDrawing>\r\n");
                            }

                            break;
                        }
                        case DrawTextBlobCanvasCommand(var skTextBlob, var f, var y, var skPaint):
                        {
                            // TODO:
                            break;
                        }
                        case DrawTextCanvasCommand(var text, var f, var y, var skPaint):
                        {
                            // TODO:
                            break;
                        }
                        case DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                        {
                            // TODO:
                            break;
                        }
                    }
                }
            }

            sb.Append($"</DrawingGroup>");

            return sb.ToString();
        }
    }
}
