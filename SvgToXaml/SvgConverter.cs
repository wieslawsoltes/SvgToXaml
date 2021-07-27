using System.Globalization;
using System.Text;
using ShimSkiaSharp;

namespace SvgToXaml
{
    public static class SvgConverter
    { 
        public static string ToPathData(SKPath path)
        {
            var sb = new StringBuilder();

            if (path.Commands is null)
            {
                return "";
            }

            if (path.FillType != SKPathFillType.Winding)
            {
                sb.Append($"F1 ");
            }

            for (var index = 0; index < path.Commands.Count; index++)
            {
                var pathCommand = path.Commands[index];

                switch (pathCommand)
                {
                    case MoveToPathCommand(var x, var y):
                    {
                        sb.Append($"{(index > 0 ? " " : "")}M{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }
                    case LineToPathCommand(var x, var y):
                    {
                        sb.Append($"{(index > 0 ? " " : "")}L{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }
                    case ArcToPathCommand(var rx, var ry, var xAxisRotate, var largeArc, var sweep, var x, var y):
                    {
                        sb.Append($"{(index > 0 ? " " : "")}A{rx.ToString(CultureInfo.InvariantCulture)},{ry.ToString(CultureInfo.InvariantCulture)} {xAxisRotate.ToString(CultureInfo.InvariantCulture)} {(largeArc == SKPathArcSize.Large ? "1" : "0")} {(sweep == SKPathDirection.Clockwise ? "1" : "0")} {x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }
                    case QuadToPathCommand(var x0, var y0, var x1, var y1):
                    {
                        sb.Append($"{(index > 0 ? " " : "")}Q{x0.ToString(CultureInfo.InvariantCulture)},{y0.ToString(CultureInfo.InvariantCulture)} {x1.ToString(CultureInfo.InvariantCulture)},{y1.ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }
                    case CubicToPathCommand(var x0, var y0, var x1, var y1, var x2, var y2):
                    {
                        sb.Append($"{(index > 0 ? " " : "")}C{x0.ToString(CultureInfo.InvariantCulture)},{y0.ToString(CultureInfo.InvariantCulture)} {x1.ToString(CultureInfo.InvariantCulture)},{y1.ToString(CultureInfo.InvariantCulture)} {x2.ToString(CultureInfo.InvariantCulture)},{y2.ToString(CultureInfo.InvariantCulture)}");
                        break;
                    }
                    case ClosePathCommand:
                    {
                        sb.Append($" Z");
                    }
                        break;
                    case AddRectPathCommand(var skRect):
                    {
                        // TODO:
                        break;
                    }
                    case AddRoundRectPathCommand(var skRect, var rx, var ry):
                    {
                        // TODO:
                        break;
                    }
                    case AddOvalPathCommand(var skRect):
                    {
                        // TODO:
                        break;
                    }
                    case AddCirclePathCommand(var f, var y, var radius):
                    {
                        // TODO:
                        break;
                    }
                    case AddPolyPathCommand(var skPoints, var close):
                    {
                        if (skPoints is not null && skPoints.Count >= 2)
                        {
                            var mx = skPoints[0].X;
                            var my = skPoints[0].Y;

                            sb.Append($"{(index > 0 ? " " : "")}M{mx.ToString(CultureInfo.InvariantCulture)},{my.ToString(CultureInfo.InvariantCulture)}");

                            for (int i = 1; i < skPoints.Count; i++)
                            {
                                var lx = skPoints[i].X;
                                var ly = skPoints[i].Y;
                                sb.Append($"{(index > 0 ? " " : "")}L{lx.ToString(CultureInfo.InvariantCulture)},{ly.ToString(CultureInfo.InvariantCulture)}");
                            }

                            if (close)
                            {
                                sb.Append($" Z");
                            }
                        }
                        break;
                    }
                }
            }

            return sb.ToString();
        }

        public static string ToHexColor(SKColor skColor, string indent = "")
        {
            return $"{indent}#{skColor.Alpha:X2}{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
        }

        public static string ToPoint(SKPoint skPoint)
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

                if (linearGradientShader.LocalMatrix is { })
                {
                    // TODO:
                }

                brush += $"{indent}<LinearGradientBrush StartPoint=\"{ToPoint(linearGradientShader.Start)}\" EndPoint=\"{ToPoint(linearGradientShader.End)}\" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\">\r\n";
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
                            var data = ToPathData(skPath);
                            //var data = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath).ToSvgPathData();

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
