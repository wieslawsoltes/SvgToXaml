//#define USE_BRUSH_TRANSFORM
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SvgToXamlConverter
{
    public static class SvgConverter
    {
        public static string NewLine = "\r\n";

        public static string ToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToHexColor(ShimSkiaSharp.SKColor skColor)
        {
            return $"#{skColor.Alpha:X2}{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
        }

        public static string ToPoint(ShimSkiaSharp.SKPoint skPoint)
        {
            return $"{ToString(skPoint.X)},{ToString(skPoint.Y)}";
        }

        public static string ToRect(ShimSkiaSharp.SKRect sKRect)
        {
            return $"{ToString(sKRect.Left)},{ToString(sKRect.Top)},{ToString(sKRect.Width)},{ToString(sKRect.Height)}";
        }

        public static string ToPoint(SkiaSharp.SKPoint skPoint)
        {
            return $"{ToString(skPoint.X)},{ToString(skPoint.Y)}";
        }

        public static string ToGradientSpreadMethod(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
        {
            switch (shaderTileMode)
            {
                default:
                case ShimSkiaSharp.SKShaderTileMode.Clamp:
                    return "Pad";

                case ShimSkiaSharp.SKShaderTileMode.Repeat:
                    return "Repeat";

                case ShimSkiaSharp.SKShaderTileMode.Mirror:
                    return "Reflect";
            }
        }

        public static string ToTileMode(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
        {
            switch (shaderTileMode)
            {
                default:
                case ShimSkiaSharp.SKShaderTileMode.Clamp:
                    return "None";

                case ShimSkiaSharp.SKShaderTileMode.Repeat:
                    return "Tile";

                case ShimSkiaSharp.SKShaderTileMode.Mirror:
                    return "FlipXY";
            };
        }
        
        private static SkiaSharp.SKMatrix AdjustMatrixLocation(SkiaSharp.SKMatrix matrix, float x, float y)
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

        public static string ToBrush(ShimSkiaSharp.ColorShader colorShader, SkiaSharp.SKRect skBounds)
        {
            var brush = "";

            brush += $"<SolidColorBrush";
            brush += $" Color=\"{ToHexColor(colorShader.Color)}\"";
            brush += $"/>{NewLine}";

            return brush;
        }

        public static string ToBrush(ShimSkiaSharp.LinearGradientShader linearGradientShader, SkiaSharp.SKRect skBounds)
        {
            var brush = "";

            var start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start);
            var end = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End);

#if !USE_BRUSH_TRANSFORM
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
#else
            if (linearGradientShader.LocalMatrix is null)
            {
                start.X = Math.Max(0f, start.X - skBounds.Location.X);
                start.Y = Math.Max(0f, start.Y - skBounds.Location.Y);
                end.X = Math.Max(0f, end.X - skBounds.Location.X);
                end.Y = Math.Max(0f, end.Y - skBounds.Location.Y);
            }
#endif

            brush += $"<LinearGradientBrush";
            brush += $" StartPoint=\"{ToPoint(start)}\"";
            brush += $" EndPoint=\"{ToPoint(end)}\"";

            if (linearGradientShader.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                brush += $" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\"";
            }

            brush += $">{NewLine}";

#if USE_BRUSH_TRANSFORM
            if (linearGradientShader.LocalMatrix is { })
            {
                // TODO: Missing Transform property on LinearGradientBrush
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);
                localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                brush += $"  <LinearGradientBrush.Transform>{NewLine}";
                brush += $"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}";
                brush += $"  </LinearGradientBrush.Transform>{NewLine}";
            }
#endif

            brush += $"  <LinearGradientBrush.GradientStops>{NewLine}";

            if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
            {
                for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                {
                    var color = ToHexColor(linearGradientShader.Colors[i]);
                    var offset = ToString(linearGradientShader.ColorPos[i]);
                    brush += $"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{NewLine}";
                }
            }

            brush += $"  </LinearGradientBrush.GradientStops>{NewLine}";
            brush += $"</LinearGradientBrush>{NewLine}";

            return brush;
        }

        public static string ToBrush(ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader, SkiaSharp.SKRect skBounds)
        {
            var brush = "";

            // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
            var startRadius = twoPointConicalGradientShader.StartRadius;

            // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
            // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
            var endRadius = twoPointConicalGradientShader.EndRadius;

            var center = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.Start);
            var gradientOrigin = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.End);

#if !USE_BRUSH_TRANSFORM
            if (twoPointConicalGradientShader.LocalMatrix is { })
            {
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);

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
#else
            if (twoPointConicalGradientShader.LocalMatrix is null)
            {
                center.X = Math.Max(0f, center.X - skBounds.Location.X);
                center.Y = Math.Max(0f, center.Y - skBounds.Location.Y);
                gradientOrigin.X = Math.Max(0f, gradientOrigin.X - skBounds.Location.X);
                gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - skBounds.Location.Y);
            }
#endif

            endRadius = endRadius / skBounds.Width;

            brush += $"<RadialGradientBrush";
            brush += $" Center=\"{ToPoint(center)}\"";
            brush += $" GradientOrigin=\"{ToPoint(gradientOrigin)}\"";
            brush += $" Radius=\"{ToString(endRadius)}\"";

            if (twoPointConicalGradientShader.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                brush += $" SpreadMethod=\"{ToGradientSpreadMethod(twoPointConicalGradientShader.Mode)}\"";
            }

            brush += $">{NewLine}";

#if USE_BRUSH_TRANSFORM
            if (twoPointConicalGradientShader.LocalMatrix is { })
            {
                // TODO: Missing Transform property on RadialGradientBrush
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);
                localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                brush += $"  <RadialGradientBrush.Transform>{NewLine}";
                brush += $"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}";
                brush += $"  </RadialGradientBrush.Transform>{NewLine}";
            }
#endif

            brush += $"  <RadialGradientBrush.GradientStops>{NewLine}";

            if (twoPointConicalGradientShader.Colors is { } && twoPointConicalGradientShader.ColorPos is { })
            {
                for (var i = 0; i < twoPointConicalGradientShader.Colors.Length; i++)
                {
                    var color = ToHexColor(twoPointConicalGradientShader.Colors[i]);
                    var offset = ToString(twoPointConicalGradientShader.ColorPos[i]);
                    brush += $"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{NewLine}";
                }
            }

            brush += $"  </RadialGradientBrush.GradientStops>{NewLine}";
            brush += $"</RadialGradientBrush>{NewLine}";

            return brush;
        }

        public static string ToBrush(ShimSkiaSharp.PictureShader pictureShader, SkiaSharp.SKRect skBounds)
        {
            var brush = "";

            if (pictureShader?.Src is null)
            {
                return brush;
            }

#if !USE_BRUSH_TRANSFORM
            if (pictureShader.LocalMatrix is { })
            {
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(pictureShader.LocalMatrix);

                if (!localMatrix.IsIdentity)
                {
                    brush += $"<!-- TODO: Transform: {ToMatrix(localMatrix)} -->{NewLine}";
                }
            }
            else
            {
                // TODO: Adjust using skBounds.Location ?
            }
#endif

            var sourceRect = pictureShader.Src.CullRect;
            var destinationRect = pictureShader.Tile;

            // TODO: Use different than Image ?
            brush += $"<VisualBrush";

            if (pictureShader.TmX != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                brush += $" TileMode=\"{ToTileMode(pictureShader.TmX)}\"";
            }

            brush += $" SourceRect=\"{ToRect(sourceRect)}\"";
            brush += $" DestinationRect=\"{ToRect(destinationRect)}\"";

            brush += $">{NewLine}";

#if USE_BRUSH_TRANSFORM
            if (pictureShader?.LocalMatrix is { })
            {
                // TODO: Missing Transform property on VisualBrush
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(pictureShader.LocalMatrix);
                localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                brush += $"  <VisualBrush.Transform>{NewLine}";
                brush += $"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}";
                brush += $"  </VisualBrush.Transform>{NewLine}";
            }
#endif

            brush += $"  <VisualBrush.Visual>{NewLine}";

            var visual = ToXaml(pictureShader.Src, generateImage: true, key: null);
            brush += visual;
            brush += $"{NewLine}";

            brush += $"  </VisualBrush.Visual>{NewLine}";
            brush += $"</VisualBrush>{NewLine}";

            return brush;
        }

        public static string ToBrush(ShimSkiaSharp.SKShader skShader, SkiaSharp.SKRect skBounds)
        {
            return skShader switch
            {
                ShimSkiaSharp.ColorShader colorShader => ToBrush(colorShader, skBounds),
                ShimSkiaSharp.LinearGradientShader linearGradientShader => ToBrush(linearGradientShader, skBounds),
                ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader => ToBrush(twoPointConicalGradientShader, skBounds),
                ShimSkiaSharp.PictureShader pictureShader => ToBrush(pictureShader, skBounds),
                _ => ""
            };
        }

        public static string ToPenLineCap(ShimSkiaSharp.SKStrokeCap strokeCap)
        {
            switch (strokeCap)
            {
                default:
                case ShimSkiaSharp.SKStrokeCap.Butt:
                    return "Flat";

                case ShimSkiaSharp.SKStrokeCap.Round:
                    return "Round";

                case ShimSkiaSharp.SKStrokeCap.Square:
                    return "Square";
            }
        }

        public static string ToPenLineJoin(ShimSkiaSharp.SKStrokeJoin strokeJoin)
        {
            switch (strokeJoin)
            {
                default:
                case ShimSkiaSharp.SKStrokeJoin.Miter:
                    return "Miter";

                case ShimSkiaSharp.SKStrokeJoin.Round:
                    return "Round";

                case ShimSkiaSharp.SKStrokeJoin.Bevel:
                    return "Bevel";
            }
        }

        public static string ToPen(ShimSkiaSharp.SKPaint skPaint, SkiaSharp.SKRect skBounds)
        {
            if (skPaint.Shader is { })
            {
                var pen = "";

                pen += $"<Pen";

                if (skPaint.Shader is ShimSkiaSharp.ColorShader colorShader)
                {
                    pen += $" Brush=\"{ToHexColor(colorShader.Color)}\"";
                }

                if (skPaint.StrokeWidth != 1.0)
                {
                    pen += $" Thickness=\"{ToString(skPaint.StrokeWidth)}\"";
                }

                if (skPaint.StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
                {
                    pen += $" LineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"";
                }

                if (skPaint.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
                {
                    pen += $" LineJoin=\"{ToPenLineJoin(skPaint.StrokeJoin)}\"";
                }

                if (skPaint.StrokeMiter != 10.0)
                {
                    pen += $" MiterLimit=\"{ToString(skPaint.StrokeMiter)}\"";
                }

                if (skPaint.Shader is not ShimSkiaSharp.ColorShader || (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } }))
                {
                    pen += $">{NewLine}";
                }
                else
                {
                    pen += $"/>{NewLine}";
                }

                if (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect dashPathEffect && dashPathEffect.Intervals is { })
                {
                    var dashes = new List<double>();

                    foreach (var interval in dashPathEffect.Intervals)
                    {
                        dashes.Add(interval / skPaint.StrokeWidth);
                    }

                    var offset = dashPathEffect.Phase / skPaint.StrokeWidth;

                    pen += $"  <Pen.DashStyle>{NewLine}";
                    pen += $"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(ToString))}\" Offset=\"{ToString(offset)}\"/>{NewLine}";
                    pen += $"  </Pen.DashStyle>{NewLine}";
                }

                if (skPaint.Shader is not ShimSkiaSharp.ColorShader)
                {
                    pen += $"  <Pen.Brush>{NewLine}";
                    pen += ToBrush(skPaint.Shader, skBounds);
                    pen += $"  </Pen.Brush>{NewLine}";
                }

                if (skPaint.Shader is not ShimSkiaSharp.ColorShader || (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } }))
                {
                    pen += $"</Pen>{NewLine}";
                }

                return pen;
            }

            return "";
        }

        public static string ToSvgPathData(SkiaSharp.SKPath path)
        {
            var data = path.ToSvgPathData();

            if (path.FillType == SkiaSharp.SKPathFillType.EvenOdd)
            {
                // EvenOdd
                data = $"F0 {data}";
            }
            else
            {
                // Nonzero 
                data = $"F1 {data}";
            }

            return data;
        }

        public static string ToMatrix(SkiaSharp.SKMatrix skMatrix)
        {
            return $"{ToString(skMatrix.ScaleX)}," +
                   $"{ToString(skMatrix.SkewY)}," +
                   $"{ToString(skMatrix.SkewX)}," +
                   $"{ToString(skMatrix.ScaleY)}," +
                   $"{ToString(skMatrix.TransX)}," +
                   $"{ToString(skMatrix.TransY)}";
        }

        public static string ToXaml(ShimSkiaSharp.SKPicture? skPicture, bool generateImage = true, string? key = null)
        {
            const byte opaqueAlpha = 255;

            var sb = new StringBuilder();

            if (generateImage)
            {
                sb.Append($"<Image{(key is null ? "" : ($" x:Key=\"{key}\""))}>{NewLine}");
                sb.Append($"  <DrawingImage>{NewLine}");
                sb.Append($"    <DrawingGroup>{NewLine}");
            }
            else
            {
                sb.Append($"<DrawingGroup{(key is null ? "" : ($" x:Key=\"{key}\""))}>{NewLine}");
            }

            if (skPicture?.Commands is { })
            {
                var totalMatrixStack = new Stack<SkiaSharp.SKMatrix>();
                var totalMatrix = SkiaSharp.SKMatrix.Identity;

                var clipPathStack = new Stack<List<SkiaSharp.SKPath>>();
                var currentClipPathList = new List<SkiaSharp.SKPath>();

                var opacityStack = new Stack<List<byte>>();
                var currentOpacityList = new List<byte>();

                foreach (var canvasCommand in skPicture.Commands)
                {
                    switch (canvasCommand)
                    {
                        case ShimSkiaSharp.ClipPathCanvasCommand(var clipPath, _, _):
                        {
                            var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                            if (path is { })
                            {
                                path.Transform(totalMatrix);
                                var clipGeometry = ToSvgPathData(path);
 
                                sb.Append($"<DrawingGroup>{NewLine}");
                                sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                                sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                                sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");

                                currentClipPathList.Add(path);
                            }

                            break;
                        }
                        case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                        {
                            var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                            var path = new SkiaSharp.SKPath();
                            path.AddRect(rect);
                            path.Transform(totalMatrix);
                            var clipGeometry = ToSvgPathData(path);

                            sb.Append($"<DrawingGroup>{NewLine}");
                            sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                            sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                            sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");

                            currentClipPathList.Add(path);
    
                            break;
                        }
                        case ShimSkiaSharp.SetMatrixCanvasCommand(var skMatrix):
                        {
                            var matrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(skMatrix);

                            totalMatrix = matrix;

                            break;
                        }
                        case ShimSkiaSharp.SaveLayerCanvasCommand(var count, var skPaint):
                        {
                            totalMatrixStack.Push(totalMatrix);
                            
                            clipPathStack.Push(currentClipPathList);
                            currentClipPathList = new List<SkiaSharp.SKPath>();
                            
                            opacityStack.Push(currentOpacityList);
                            currentOpacityList = new List<byte>();

                            // TODO:
                            if (skPaint is { } && skPaint.Shader is null && skPaint.ColorFilter is null && skPaint.ImageFilter is null)
                            {
                                if (skPaint.Color is { } skColor && skColor.Alpha < opaqueAlpha)
                                {
                                    sb.Append($"<DrawingGroup Opacity=\"{ToString(skColor.Alpha / 255.0)}\">{NewLine}");
                                    currentOpacityList.Add(skColor.Alpha);
                                }
                                else
                                {
                                    currentOpacityList.Add(opaqueAlpha);
                                }
                            }
                            else
                            {
                                currentOpacityList.Add(opaqueAlpha);
                            }

                            break;
                        }
                        case ShimSkiaSharp.SaveCanvasCommand:
                        {
                            totalMatrixStack.Push(totalMatrix);
                            
                            clipPathStack.Push(currentClipPathList);
                            currentClipPathList = new List<SkiaSharp.SKPath>();
                            
                            opacityStack.Push(currentOpacityList);
                            currentOpacityList = new List<byte>();
                            
                            break;
                        }
                        case ShimSkiaSharp.RestoreCanvasCommand:
                        {
                            foreach (var clipPath in currentClipPathList)
                            {
                                sb.Append($"</DrawingGroup>{NewLine}");
                            }

                            currentClipPathList.Clear();

                            if (clipPathStack.Count > 0)
                            {
                                currentClipPathList = clipPathStack.Pop();
                            }

                            foreach (var totalOpacity in currentOpacityList)
                            {
                                if (totalOpacity < opaqueAlpha)
                                {
                                    sb.Append($"</DrawingGroup>{NewLine}");
                                }
                            }

                            currentOpacityList.Clear();

                            if (opacityStack.Count > 0)
                            {
                                currentOpacityList = opacityStack.Pop();
                            }

                            if (totalMatrixStack.Count > 0)
                            {
                                totalMatrix = totalMatrixStack.Pop();
                            }

                            break;
                        }
                        case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                        {
                            if (!totalMatrix.IsIdentity)
                            {
                                sb.Append($"<DrawingGroup>{NewLine}");
                                
                                sb.Append($"  <DrawingGroup.Transform>{NewLine}");
                                sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(totalMatrix)}\"/>{NewLine}");
                                sb.Append($"  </DrawingGroup.Transform>{NewLine}");
                            }

                            sb.Append($"<GeometryDrawing");

                            if ((skPaint.Style == ShimSkiaSharp.SKPaintStyle.Fill || skPaint.Style == ShimSkiaSharp.SKPaintStyle.StrokeAndFill) && skPaint.Shader is ShimSkiaSharp.ColorShader colorShader)
                            {
                                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
                            }

                            var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                            var geometry = ToSvgPathData(path);

                            sb.Append($" Geometry=\"{geometry}\"");

                            var brush = default(string);
                            var pen = default(string);

                            if ((skPaint.Style == ShimSkiaSharp.SKPaintStyle.Fill || skPaint.Style == ShimSkiaSharp.SKPaintStyle.StrokeAndFill) && skPaint.Shader is not ShimSkiaSharp.ColorShader)
                            {
                                if (skPaint.Shader is { })
                                {
                                    brush = ToBrush(skPaint.Shader, path.Bounds);
                                }
                            }

                            if (skPaint.Style == ShimSkiaSharp.SKPaintStyle.Stroke || skPaint.Style == ShimSkiaSharp.SKPaintStyle.StrokeAndFill)
                            {
                                if (skPaint.Shader is { })
                                {
                                    pen = ToPen(skPaint, path.Bounds);
                                }
                            }

                            if (brush is not null || pen is not null)
                            {
                                sb.Append($">{NewLine}");
                            }
                            else
                            {
                                sb.Append($"/>{NewLine}");
                            }

                            if (brush is { })
                            {
                                sb.Append($"  <GeometryDrawing.Brush>{NewLine}");
                                sb.Append($"{brush}");
                                sb.Append($"  </GeometryDrawing.Brush>{NewLine}");
                            }

                            if (pen is { })
                            {
                                sb.Append($"  <GeometryDrawing.Pen>{NewLine}");
                                sb.Append($"{pen}");
                                sb.Append($"  </GeometryDrawing.Pen>{NewLine}");
                            }

                            if (brush is not null || pen is not null)
                            {
                                sb.Append($"</GeometryDrawing>{NewLine}");
                            }

                            if (!totalMatrix.IsIdentity)
                            {
                                sb.Append($"</DrawingGroup>{NewLine}");
                            }

                            break;
                        }
                        case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var f, var y, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case ShimSkiaSharp.DrawTextCanvasCommand(var text, var f, var y, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                    }
                }
                                
                foreach (var clipPath in currentClipPathList)
                {
                    sb.Append($"</DrawingGroup>{NewLine}");
                }

                currentClipPathList.Clear();

                foreach (var totalOpacity in currentOpacityList)
                {
                    if (totalOpacity < opaqueAlpha)
                    {
                        sb.Append($"</DrawingGroup>{NewLine}");
                    }
                }

                currentOpacityList.Clear();
            }

            if (generateImage)
            {
                sb.Append($"    </DrawingGroup>{NewLine}");
                sb.Append($"  </DrawingImage>{NewLine}");
                sb.Append($"</Image>");
            }
            else
            {
                sb.Append($"</DrawingGroup>");
            }

            return sb.ToString();
        }

        public static string ToXaml(List<string> paths, bool generateImage = false, bool generateStyles = true)
        {
            var sb = new StringBuilder();

            if (generateStyles)
            {
                sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{NewLine}");
                sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{NewLine}");
                sb.Append($"  <Style>{NewLine}");
                sb.Append($"    <Style.Resources>{NewLine}");
            }

            foreach (var path in paths)
            {
                var svg = new Svg.Skia.SKSvg();
                svg.Load(path);
                if (svg.Model is null)
                {
                    continue;
                }

                var xaml = ToXaml(svg.Model, generateImage: generateImage, key: generateStyles ? $"_{CreateKey(path)}" : null);
                sb.Append($"<!-- {Path.GetFileName(path)} -->{NewLine}");
                sb.Append(xaml);
                sb.Append(NewLine);
            }

            if (generateStyles)
            {
                sb.Append($"    </Style.Resources>{NewLine}");
                sb.Append($"  </Style>{NewLine}");
                sb.Append($"</Styles>");
            }

            return sb.ToString();
        }

        public static string CreateKey(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string key = name.Replace("-", "_");
            return $"_{key}";
        }

        public static string Format(string xml)
        {
            try
            {
                using var ms = new MemoryStream();
                using var writer = new XmlTextWriter(ms, Encoding.UTF8);
                var document = new XmlDocument();
                document.LoadXml(xml);
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                document.WriteContentTo(writer);
                writer.Flush();
                ms.Flush();
                ms.Position = 0;
                using var sReader = new StreamReader(ms);
                return sReader.ReadToEnd();
            }
            catch
            {
                // ignored
            }

            return "";
        }
    }
}
