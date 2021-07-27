using System.Globalization;
using System.Text;
using ShimSkiaSharp;

namespace SvgToXaml
{
    public static class SvgConverter
    { 
        public static void ToPathData(SKPath path, StringBuilder sb)
        {
            if (path.FillType != SKPathFillType.Winding)
            {
                sb.Append($"F1 ");
            }

            if (path.Commands is null)
            {
                return;
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
                        sb.Append($"{(index > 0 ? " " : "")}A{rx.ToString(CultureInfo.InvariantCulture)},{ry.ToString(CultureInfo.InvariantCulture)} {xAxisRotate.ToString(CultureInfo.InvariantCulture)}, {(largeArc == SKPathArcSize.Large ? "1" : "0")}, {(sweep == SKPathDirection.Clockwise ? "1" : "0")}, {x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)});");
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
        }

        public static string ToXaml(SKPicture? model)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup>\r\n");

            if (model?.Commands is { })
            {
                foreach (var canvasCommand in model.Commands)
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
                            var sbPath = new StringBuilder();

                            ToPathData(skPath, sbPath);

                            var data = sbPath.ToString();

                            if (skPaint.Style == SKPaintStyle.Fill)
                            {
                                if (skPaint.Shader is ColorShader colorShader)
                                {
                                    var brush =
                                        $"#{colorShader.Color.Alpha:X2}{colorShader.Color.Red:X2}{colorShader.Color.Green:X2}{colorShader.Color.Blue:X2}";
                                    sb.Append($"  <GeometryDrawing Brush=\"{brush}\" Geometry=\"{data}\"/>\r\n");
                                }
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
