using System.Globalization;
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
                var brush = $"{indent}<SolidColorBrush Color=\"{ToHexColor(colorShader.Color)}\"/>\r\n";
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

                brush += $"{indent}<LinearGradientBrush StartPoint=\"{ToPoint(start)}\" EndPoint=\"{ToPoint(end)}\" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\">\r\n";
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
                            var data = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath).ToSvgPathData();

                            var brush = default(string);
                            var pen = default(string);

                            if (skPaint.Style == SKPaintStyle.Fill || skPaint.Style == SKPaintStyle.StrokeAndFill)
                            {
                                if (skPaint.Shader is { })
                                {
                                    brush = ToBrush(skPaint.Shader, "      ");
                                }
                            }

                            if (skPaint.Style == SKPaintStyle.Stroke || skPaint.Style == SKPaintStyle.StrokeAndFill)
                            {
                                if (skPaint.Shader is { })
                                {
                                    pen = $"      <Pen Thickness=\"{skPaint.StrokeWidth.ToString(CultureInfo.InvariantCulture)}\">\r\n" +
                                          $"        <Pen.Brush>\r\n" +
                                          ToBrush(skPaint.Shader, "          ") +
                                          $"        </Pen.Brush>\r\n" +
                                          $"      </Pen>\r\n";
                                }
                            }

                            sb.Append($"  <GeometryDrawing Geometry=\"{data}\">\r\n");

                            if (brush is { })
                            {
                                sb.Append($"    <GeometryDrawing.Brush>\r\n");
                                sb.Append($"{brush}");
                                sb.Append($"    </GeometryDrawing.Brush>\r\n");
                            }
                            
                            if (pen is { })
                            {
                                sb.Append($"    <GeometryDrawing.Pen>\r\n");
                                sb.Append($"{pen}");
                                sb.Append($"    </GeometryDrawing.Pen>\r\n");
                            }

                            sb.Append($"  </GeometryDrawing>\r\n");

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
