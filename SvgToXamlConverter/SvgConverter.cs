using System;
using System.Collections.Generic;
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
            var brush = new SolidColorBrush
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = null,
                Color = colorShader.Color
            };

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform
            };
 
            return brush.Generate(context);
        }

        private string ToBrush(ShimSkiaSharp.LinearGradientShader linearGradientShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var brush = ToBrushModel(linearGradientShader, skBounds, key);

            var context = new GeneratorContext()
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform
            };
 
            return brush.Generate(context);
        }

        private LinearGradientBrush ToBrushModel(ShimSkiaSharp.LinearGradientShader linearGradientShader, SkiaSharp.SKRect skBounds, string? key)
        {
            var brush = new LinearGradientBrush
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = linearGradientShader.LocalMatrix is null
                    ? null
                    : Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value),
                Start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start),
                End = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End),
                Mode = linearGradientShader.Mode
            };

            if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
            {
                for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                {
                    var color = linearGradientShader.Colors[i];
                    var offset = linearGradientShader.ColorPos[i];
                    brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
                }
            }

            return brush;
        }

        private string ToBrush(ShimSkiaSharp.RadialGradientShader radialGradientShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var brush = ToBrushModel(radialGradientShader, skBounds, key);

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform
            };
 
            return brush.Generate(context);
        }

        private Brush ToBrushModel(ShimSkiaSharp.RadialGradientShader radialGradientShader, SkiaSharp.SKRect skBounds, string? key)
        {
            var brush = new RadialGradientBrush
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = radialGradientShader.LocalMatrix is null
                    ? null
                    : Svg.Skia.SkiaModelExtensions.ToSKMatrix(radialGradientShader.LocalMatrix.Value),
                Center = Svg.Skia.SkiaModelExtensions.ToSKPoint(radialGradientShader.Center),
                Radius = radialGradientShader.Radius,
                Mode = radialGradientShader.Mode
            };

            if (radialGradientShader.Colors is { } && radialGradientShader.ColorPos is { })
            {
                for (var i = 0; i < radialGradientShader.Colors.Length; i++)
                {
                    var color = radialGradientShader.Colors[i];
                    var offset = radialGradientShader.ColorPos[i];
                    brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
                }
            }

            return brush;
        }

        private string ToBrush(ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var brush = ToBrushModel(twoPointConicalGradientShader, skBounds, key);

            var context = new GeneratorContext()
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform
            };
 
            return brush.Generate(context);
        }

        private Brush ToBrushModel(ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader, SkiaSharp.SKRect skBounds, string? key)
        {
            var brush = new TwoPointConicalGradientBrush()
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = twoPointConicalGradientShader.LocalMatrix is null
                    ? null
                    : Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value),
                Start = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.Start),
                End = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.End),
                StartRadius = twoPointConicalGradientShader.StartRadius,
                EndRadius = twoPointConicalGradientShader.EndRadius,
                Mode = twoPointConicalGradientShader.Mode
            };

            if (twoPointConicalGradientShader.Colors is { } && twoPointConicalGradientShader.ColorPos is { })
            {
                for (var i = 0; i < twoPointConicalGradientShader.Colors.Length; i++)
                {
                    var color = twoPointConicalGradientShader.Colors[i];
                    var offset = twoPointConicalGradientShader.ColorPos[i];
                    brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
                }
            }

            return brush;
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
                        sb.Append($"<!-- TODO: Transform: {XamlConverter.ToMatrix(localMatrix)} -->{NewLine}");
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
            sb.Append($"<VisualBrush{XamlConverter.ToKey(key)}");

            if (pictureShader.TmX != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" TileMode=\"{XamlConverter.ToTileMode(pictureShader.TmX)}\"");
            }

            if (UseCompatMode)
            {
                sb.Append($" Viewport=\"{XamlConverter.ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                sb.Append($" Viewbox=\"{XamlConverter.ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
            }
            else
            {
                sb.Append($" SourceRect=\"{XamlConverter.ToRect(sourceRect)}\"");
                sb.Append($" DestinationRect=\"{XamlConverter.ToRect(destinationRect)}\"");
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
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{NewLine}");
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

        private Brush? ToBrushModel(ShimSkiaSharp.PictureShader pictureShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var brush = new PictureBrush
            {
                Key = key,
                Bounds = skBounds,
                Picture = null, // TODO:
                CullRect =  pictureShader.Src?.CullRect ?? ShimSkiaSharp.SKRect.Empty,
                Tile = pictureShader.Tile,
                TileMode = pictureShader.TmX
            };

            return brush;
        }

        private string ToBrush(ShimSkiaSharp.SKShader skShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            return skShader switch
            {
                ShimSkiaSharp.ColorShader colorShader => ToBrush(colorShader, skBounds, key),
                ShimSkiaSharp.LinearGradientShader linearGradientShader => ToBrush(linearGradientShader, skBounds, key),
                ShimSkiaSharp.RadialGradientShader radialGradientShader => ToBrush(radialGradientShader, skBounds, key),
                ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader => ToBrush(twoPointConicalGradientShader, skBounds, key),
                ShimSkiaSharp.PictureShader pictureShader => ToBrush(pictureShader, skBounds, key),
                _ => ""
            };
        }

        private Brush? ToBrushModel(ShimSkiaSharp.SKShader skShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            return skShader switch
            {
                ShimSkiaSharp.ColorShader colorShader => ToBrushModel(colorShader, skBounds, key),
                ShimSkiaSharp.LinearGradientShader linearGradientShader => ToBrushModel(linearGradientShader, skBounds, key),
                ShimSkiaSharp.RadialGradientShader radialGradientShader => ToBrushModel(radialGradientShader, skBounds, key),
                ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader => ToBrushModel(twoPointConicalGradientShader, skBounds, key),
                ShimSkiaSharp.PictureShader pictureShader => ToBrushModel(pictureShader, skBounds, key),
                _ => null
            };
        }

        private string ToPen(ShimSkiaSharp.SKPaint skPaint, SkiaSharp.SKRect skBounds, string? key = null)
        {
            if (skPaint.Shader is null)
            {
                return "";
            }

            var pen = ToPenModel(skPaint, skBounds, key);

            var context = new GeneratorContext()
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform
            };
 
            return pen.Generate(context);
        }

        private Pen ToPenModel(ShimSkiaSharp.SKPaint skPaint, SkiaSharp.SKRect skBounds, string? key)
        {
            var pen = new Pen()
            {
                Key = key,
                Bounds = skBounds,
                Brush = skPaint.Shader is { } 
                    ? ToBrushModel(skPaint.Shader, skBounds) 
                    : null,
                StrokeWidth = skPaint.StrokeWidth,
                StrokeCap = skPaint.StrokeCap,
                StrokeJoin = skPaint.StrokeJoin,
                StrokeMiter = skPaint.StrokeMiter,
                Dashes = skPaint.PathEffect is ShimSkiaSharp.DashPathEffect(var intervals, var phase) { Intervals: { } }
                    ? new Dashes() { Intervals = intervals, Phase = phase }
                    : null
            };

            return pen;
        }

        private void ToXamlGeometryDrawing(SkiaSharp.SKPath path, ShimSkiaSharp.SKPaint skPaint, StringBuilder sb, Resources? resources = null, bool reuseExistingResources = false)
        {
            sb.Append($"<GeometryDrawing");

            var isFilled = skPaint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Fill;
            var isStroked = skPaint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Stroke;

            if (isFilled && skPaint.Shader is ShimSkiaSharp.ColorShader colorShader && resources is null)
            {
                sb.Append($" Brush=\"{XamlConverter.ToHexColor(colorShader.Color)}\"");
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

            sb.Append($" Geometry=\"{XamlConverter.ToSvgPathData(path)}\"");

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

            sb.Append($"<DrawingGroup{XamlConverter.ToKey(key)}>{NewLine}");

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

                        var clipGeometry = XamlConverter.ToSvgPathData(path);

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

                        var clipGeometry = XamlConverter.ToSvgPathData(path);

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
                        sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(matrix)}\"/>{NewLine}");
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
                        if (path.IsEmpty)
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
                            sb.Append($"<DrawingGroup Opacity=\"{XamlConverter.ToString(skColor.Alpha / 255.0)}\">{NewLine}");
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
                sb.Append($"<Image{XamlConverter.ToKey(key)}");
                // sb.Append(UseCompatMode
                //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                //     : $" xmlns=\"https://github.com/avaloniaui\"");
                // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
                sb.Append($">{NewLine}");
            }
            else
            {
                sb.Append($"<Image{XamlConverter.ToKey(key)}");
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
