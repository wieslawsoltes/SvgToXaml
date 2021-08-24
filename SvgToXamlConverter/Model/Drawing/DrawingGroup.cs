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

            var paintStack = new Stack<ShimSkiaSharp.SKPaint?>();
            var currentPaint = default(ShimSkiaSharp.SKPaint?);

            var layerStack = new Stack<DrawingGroup>();
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
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                        if (path is null)
                        {
                            break;
                        }

                        var newLayer = new DrawingGroup
                        {
                            ClipGeometry = path
                        };

                        //Debug($"ClipPath({currentCount})", currentCount);
                        currentLayer.Children.Add(newLayer);
                        layerStack.Push(currentLayer);
                        currentLayer = newLayer;
                        currentClipPath = path;
                        Debug($"SET({currentCount}) currentClipPath", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                    {
                        var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                        var path = new SkiaSharp.SKPath();
                        path.AddRect(rect);

                        var newLayer = new DrawingGroup
                        {
                            ClipGeometry = path
                        };

                        //Debug($"ClipRect({currentCount})", currentCount);
                        currentLayer.Children.Add(newLayer);
                        layerStack.Push(currentLayer);
                        currentLayer = newLayer;
                        currentClipPath = path;
                        Debug($"SET({currentCount}) currentClipPath", currentCount);
                        
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

                        var newLayer = new DrawingGroup
                        {
                            Transform = matrix
                        };

                        //Debug($"SetMatrix({currentCount})", currentCount);
                        currentLayer.Children.Add(newLayer);
                        layerStack.Push(currentLayer);
                        currentLayer = newLayer;
                        currentTotalMatrix = matrix;
                        Debug($"SET({currentCount}) currentTotalMatrix", currentCount);
                        
                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(var count, var skPaint):
                    {
                        currentCount = count;

                        //Debug($"SaveLayer({currentCount})", currentCount);

                        if (skPaint is null)
                        {
                            break;
                        }
                        
                        totalMatrixStack.Push(currentTotalMatrix);
                        currentTotalMatrix = default;

                        clipPathStack.Push(currentClipPath);
                        currentClipPath = default;

                        var newLayer = new DrawingGroup
                        {
                        };

                        //currentLayer.Children.Add(newLayer);
                        layerStack.Push(currentLayer);
                        currentLayer = newLayer;
                        currentPaint = skPaint;
                        Debug($"SET({currentCount}) currentPaint", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.SaveCanvasCommand(var count):
                    {
                        currentCount = count;

                        //Debug($"Save({currentCount})", currentCount);

                        totalMatrixStack.Push(currentTotalMatrix);
                        currentTotalMatrix = default;

                        clipPathStack.Push(currentClipPath);
                        currentClipPath = default;

                        paintStack.Push(currentPaint);
                        currentPaint = default;

                        //layerStack.Push(currentLayer);

                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand(var count):
                    {
                        currentCount = count;

                        //Debug($"Restore({currentCount})", currentCount);

                        if (currentPaint is { })
                        {
                            Debug($"GET({currentCount}) currentPaint", currentCount);

                            var skPaint = currentPaint;

                            var content = currentLayer;
                            currentLayer = layerStack.Pop();

                            var isMaskGroup = skPaint.Shader is null 
                                              && skPaint.ColorFilter is null 
                                              && skPaint.ImageFilter is null 
                                              && skPaint.Color is { } skMaskStartColor 
                                              && skMaskStartColor.Equals(s_transparentBlack);
                            if (isMaskGroup)
                            {
                                if (content is { })
                                {
                                    currentLayer.Children.Add(content);
                                }
                            }

                            var isMaskBrush = skPaint.Shader is null 
                                              && skPaint.ColorFilter is { }
                                              && skPaint.ImageFilter is null 
                                              && skPaint.Color is { } skMaskEndColor 
                                              && skMaskEndColor.Equals(s_transparentBlack);
                            if (isMaskBrush)
                            {
                                var drawing = new DrawingGroup
                                {
                                    OpacityMask = new PictureBrush
                                    {
                                        Picture = new Image(new DrawingImage(content)),
                                        TileMode = ShimSkiaSharp.SKShaderTileMode.Clamp
                                    }
                                };

                                currentLayer.Children.Add(drawing);  
                            }

                            var isOpacityGroup = skPaint.Shader is null 
                                                 && skPaint.ColorFilter is null 
                                                 && skPaint.ImageFilter is null 
                                                 && skPaint.Color is { Alpha: < OpaqueAlpha };
                            if (isOpacityGroup)
                            {
                                if (skPaint.Color is { } skColor)
                                {
                                    if (content is { })
                                    {
                                        content.Opacity = skColor.Alpha / 255.0;
                                        currentLayer.Children.Add(content);
                                    }
                                }
                            }

                            var isFilterGroup = skPaint.Shader is null 
                                                && skPaint.ColorFilter is null
                                                && skPaint.ImageFilter is { } 
                                                && skPaint.Color is { } skFilterColor
                                                && skFilterColor.Equals(s_transparentBlack);
                            if (isFilterGroup)
                            {
                                if (content is { })
                                {
                                    var drawing = new DrawingGroup();

                                    drawing.Children.Add(content);

                                    currentLayer.Children.Add(drawing);
                                }
                            }

                            if (!isMaskGroup && !isMaskBrush && !isOpacityGroup && !isFilterGroup)
                            {
                                if (content is { })
                                {
                                    currentLayer.Children.Add(content);
                                }
                            }
                        }
                        currentPaint = default;
                        if (paintStack.Count > 0)
                        {
                            currentPaint = paintStack.Pop();
                        }

                        if (currentClipPath is { })
                        {
                            Debug($"GET({currentCount}) currentClipPath", currentCount);
                            currentLayer = layerStack.Pop();
                        }
                        currentClipPath = default;
                        if (clipPathStack.Count > 0)
                        {
                            currentClipPath = clipPathStack.Pop();
                        }

                        if (currentTotalMatrix is { })
                        {
                            Debug($"GET({currentCount}) currentTotalMatrix", currentCount);
                            currentLayer = layerStack.Pop();
                        }
                        currentTotalMatrix = default;
                        if (totalMatrixStack.Count > 0)
                        {
                            currentTotalMatrix = totalMatrixStack.Pop();
                           
                        }

                        //currentLayer = layerStack.Pop();

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

                        //Debug($"Text='{text}'", currentCount);

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

                        //Debug($"DrawTextOnPath({currentCount})", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:

                        //Debug($"DrawTextBlob({currentCount})", currentCount);

                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:

                        //Debug($"DrawImage({currentCount})", currentCount);

                        break;
                    }
                }
            }
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
