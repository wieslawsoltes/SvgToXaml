using System.Collections.Generic;
using System.Text;

namespace SvgToXamlConverter
{
    public record DrawingGroup : Drawing
    {
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

        public ShimSkiaSharp.SKPicture? Picture { get; }

        public double? Opacity { get; set; }

        public SkiaSharp.SKMatrix? Transform { get; set; }

        public SkiaSharp.SKPath? ClipGeometry { get; set; }

        public Brush? OpacityMask { get; set; }

        public List<Drawing> Children { get; } = new();

        public DrawingGroup(ShimSkiaSharp.SKPicture? picture = null, ResourceDictionary? resources = null, string? key = null)
        {
            Key = key;
            Picture = picture;

            if (Picture?.Commands is null)
            {
                return;
            }
            
            int currentCount = 0;
            
            var totalMatrixStack = new Stack<SkiaSharp.SKMatrix?>();
            var currentTotalMatrix = default(SkiaSharp.SKMatrix?);

            var clipPathStack = new Stack<SkiaSharp.SKPath?>();
            var currentClipPath = default(SkiaSharp.SKPath?);

            var layersStack = new Stack<(DrawingGroup Layer, int Count)>();
            var currentLayer = this;

            void Debug(string message, int count)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(new string(' ', count * 2) + message);
#endif
            }

            foreach (var canvasCommand in Picture.Commands)
            {
                switch (canvasCommand)
                {
                    case ShimSkiaSharp.ClipPathCanvasCommand(var clipPath, _, _):
                    {
                        Debug($"ClipPath({currentCount})", currentCount);

                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                        if (path is null)
                        {
                            break;
                        }

                        var newLayer = new DrawingGroup
                        {
                            ClipGeometry = path
                        };

                        currentLayer.Children.Add(newLayer);
                        layersStack.Push((currentLayer, currentCount));
                        currentLayer = newLayer;

                        break;
                    }
                    case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                    {
                        Debug($"ClipRect({currentCount})", currentCount);

                        var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                        var path = new SkiaSharp.SKPath();
                        path.AddRect(rect);

                        var newLayer = new DrawingGroup
                        {
                            ClipGeometry = path
                        };

                        currentLayer.Children.Add(newLayer);
                        layersStack.Push((currentLayer, currentCount));
                        currentLayer = newLayer;

                        break;
                    }
                    case ShimSkiaSharp.SetMatrixCanvasCommand(var skMatrix):
                    {
                        Debug($"SetMatrix({currentCount})", currentCount);

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

                        var newLayer = new DrawingGroup
                        {
                            Transform = matrix
                        };

                        currentLayer.Children.Add(newLayer);
                        layersStack.Push((currentLayer, currentCount));
                        currentLayer = newLayer;

                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(var count, var skPaint):
                    {
                        currentCount = count;

                        Debug($"SaveLayer({currentCount})", currentCount);

                        var newLayer = new DrawingGroup
                        {
                        };

                        currentLayer.Children.Add(newLayer);
                        layersStack.Push((currentLayer, currentCount));
                        currentLayer = newLayer;
                        
                        break;
                    }
                    case ShimSkiaSharp.SaveCanvasCommand(var count):
                    {
                        currentCount = count;

                        Debug($"Save({currentCount})", currentCount);

                        totalMatrixStack.Push(currentTotalMatrix);
                        currentTotalMatrix = default;

                        clipPathStack.Push(currentClipPath);
                        currentClipPath = default;
                        
                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand(var count):
                    {
                        currentCount = count;

                        Debug($"Restore({currentCount})", currentCount);

                        if (clipPathStack.Count > 0)
                        {
                            currentClipPath = clipPathStack.Pop();
                        }
                        else
                        {
                            currentClipPath = default;
                        }

                        if (totalMatrixStack.Count > 0)
                        {
                            currentTotalMatrix = totalMatrixStack.Pop();
                        }
                        else
                        {
                            currentTotalMatrix = default;
                        }

                        while (true)
                        {
                            if (layersStack.Count > 0)
                            {
                                var layerPeek = layersStack.Peek();

                                if (layerPeek.Count == currentCount)
                                {
                                    currentLayer = layerPeek.Layer;
                                    layersStack.Pop();
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        break;
                    }
                    case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                    {
                        Debug($"DrawPath({currentCount})", currentCount);

                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        var geometryDrawing = new GeometryDrawing(skPaint, path, resources);

                        currentLayer.Children.Add(geometryDrawing);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextCanvasCommand(var text, var x, var y, var skPaint):
                    {
                        Debug($"DrawText({currentCount})", currentCount);

                        var paint = Svg.Skia.SkiaModelExtensions.ToSKPaint(skPaint);
                        var path = paint.GetTextPath(text, x, y);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        Debug($"Text='{text}'", currentCount);

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Center)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width / 2f, 0f));
                        }

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Right)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width, 0f));
                        }

                        var geometryDrawing = new GeometryDrawing(skPaint, path, resources);

                        currentLayer.Children.Add(geometryDrawing);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                    {
                        // TODO:

                        Debug($"DrawTextOnPath({currentCount})", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:

                        Debug($"DrawTextBlob({currentCount})", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:

                        Debug($"DrawImage({currentCount})", currentCount);

                        break;
                    }
                }
            }
/*
            var sb = this;

            var totalMatrixStack = new Stack<(SkiaSharp.SKMatrix? Matrix, DrawingGroup Builder)>();
            var currentTotalMatrix = default((SkiaSharp.SKMatrix? Matrix, DrawingGroup Builder));

            var clipPathStack = new Stack<(SkiaSharp.SKPath? Path, DrawingGroup Builder)>();
            var currentClipPath = default((SkiaSharp.SKPath? Path, DrawingGroup Builder));

            var layersStack = new Stack<(DrawingGroup Builder, LayerType Type, object? Value)?>();

            int currentCount = 0;
            
            foreach (var canvasCommand in Picture.Commands)
            {
                switch (canvasCommand)
                {
                    case ShimSkiaSharp.ClipPathCanvasCommand(var clipPath, _, _):
                    {
                        Debug($"ClipPath({currentCount})", currentCount);

                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                        if (path is null)
                        {
                            break;
                        }

                        var drawing = new DrawingGroup
                        {
                            ClipGeometry = path
                        };

                        currentClipPath = (path, sb);
                        sb.Children.Add(drawing);
                        sb = drawing;

                        break;
                    }
                    case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                    {
                        Debug($"ClipRect({currentCount})", currentCount);

                        var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                        var path = new SkiaSharp.SKPath();
                        path.AddRect(rect);

                        var drawing = new DrawingGroup
                        {
                            ClipGeometry = path
                        };

                        currentClipPath = (path, sb);
                        sb.Children.Add(drawing);
                        sb = drawing;

                        break;
                    }
                    case ShimSkiaSharp.SetMatrixCanvasCommand(var skMatrix):
                    {
                        Debug($"SetMatrix({currentCount})", currentCount);

                        var matrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(skMatrix);
                        if (matrix.IsIdentity)
                        {
                            break;
                        }

                        var previousMatrixList = new List<SkiaSharp.SKMatrix>();

                        foreach (var totalMatrixList in totalMatrixStack)
                        {
                            if (totalMatrixList.Matrix is { } totalMatrix)
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

                        var drawing = new DrawingGroup
                        {
                            Transform = matrix
                        };

                        currentTotalMatrix = (matrix, sb);
                        sb.Children.Add(drawing);
                        sb = drawing;

                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(var count, var skPaint):
                    {
                        currentCount = count;

                        Debug($"SaveLayer({currentCount})", currentCount);

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
                        currentCount = count;

                        Debug($"Save({currentCount})", currentCount);

                        EmptyLayer();
                        Save(count);

                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand(var count):
                    {
                        currentCount = count;

                        Debug($"Restore({currentCount})", currentCount);

                        Restore(count);

                        break;
                    }
                    case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                    {
                        Debug($"DrawPath({currentCount})", currentCount);

                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        sb.Children.Add(new GeometryDrawing(skPaint, path, resources));

                        break;
                    }
                    case ShimSkiaSharp.DrawTextCanvasCommand(var text, var x, var y, var skPaint):
                    {
                        Debug($"DrawText({currentCount})", currentCount);

                        var paint = Svg.Skia.SkiaModelExtensions.ToSKPaint(skPaint);
                        var path = paint.GetTextPath(text, x, y);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        Debug($"Text='{text}'", currentCount);

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Center)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width / 2f, 0f));
                        }

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Right)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width, 0f));
                        }

                        sb.Children.Add(new GeometryDrawing(skPaint, path, resources));

                        break;
                    }
                    case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                    {
                        // TODO:

                        Debug($"DrawTextOnPath({currentCount})", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:

                        Debug($"DrawTextBlob({currentCount})", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:

                        Debug($"DrawImage({currentCount})", currentCount);

                        break;
                    }
                }
            }

            void Debug(string message, int count)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(new string(' ', count * 2) + message);
#endif
            }

            void EmptyLayer()
            {
                layersStack.Push(null);
            }

            void SaveLayer(LayerType type, object? value, int count)
            {
                layersStack.Push((sb, type, value));
                sb = new DrawingGroup();
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
                var content = sb;

                sb = builder;

                switch (type)
                {
                    case LayerType.UnknownPaint:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        if (content is { })
                        {
                            sb.Children.Add(content);
                        }

                        break;
                    }
                    case LayerType.MaskGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        if (content is { })
                        {
                            sb.Children.Add(content);
                        }

                        break;
                    }
                    case LayerType.MaskBrush:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        var drawing = new DrawingGroup
                        {
                            OpacityMask = new PictureBrush
                            {
                                Picture = new Image(new DrawingImage(content)),
                                TileMode = ShimSkiaSharp.SKShaderTileMode.Clamp
                            }
                        };

                        sb.Children.Add(drawing);

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
                            if (content is { })
                            {
                                content.Opacity = skColor.Alpha / 255.0;

                                sb.Children.Add(content);
                            }
                        }

                        break;
                    }
                    case LayerType.FilterGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        if (content is { })
                        {
                            var drawing = new DrawingGroup();

                            drawing.Children.Add(content);

                            sb.Children.Add(drawing);
                        }

                        break;
                    }
                }
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

                if (currentClipPath.Builder is { })
                {
                    sb = currentClipPath.Builder;

                    //Debug($"EndClipPath()", currentCount);
                }

                currentClipPath = default;

                if (clipPathStack.Count > 0)
                {
                    currentClipPath = clipPathStack.Pop();
                }

                // matrix

                if (currentTotalMatrix.Builder is { })
                {
                    sb = currentTotalMatrix.Builder;

                    //Debug($"EndMatrix()", currentCount);
                }

                currentTotalMatrix = default;

                if (totalMatrixStack.Count > 0)
                {
                    currentTotalMatrix = totalMatrixStack.Pop();
                }
            }
*/
        }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup{XamlConverter.ToKey(Key)}");

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Opacity is { })
            {
                sb.Append($" Opacity=\"{XamlConverter.ToString(Opacity.Value)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (ClipGeometry is { })
            {
                var clipGeometry = XamlConverter.ToSvgPathData(ClipGeometry);

                sb.Append($"  <DrawingGroup.ClipGeometry>{context.NewLine}");
                sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{context.NewLine}");
                sb.Append($"  </DrawingGroup.ClipGeometry>{context.NewLine}");
            }

            if (Transform is { })
            {
                var matrix = Transform.Value;

                sb.Append($"  <DrawingGroup.Transform>{context.NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(matrix)}\"/>{context.NewLine}");
                sb.Append($"  </DrawingGroup.Transform>{context.NewLine}");
            }

            if (OpacityMask is { })
            {
                sb.Append($"<DrawingGroup.OpacityMask>{context.NewLine}");
                sb.Append(OpacityMask.Generate(context));
                sb.Append($"</DrawingGroup.OpacityMask>{context.NewLine}");
            }

            foreach (var child in Children)
            {
                sb.Append(child.Generate(context));
            }

            sb.Append($"</DrawingGroup>");

            return sb.ToString();
        }
    }
}
