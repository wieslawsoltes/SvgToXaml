using System.Collections.Generic;

namespace SvgToXamlConverter
{
    public record DrawingGroup : Drawing
    {
        private const byte OpaqueAlpha = 255;

        private static readonly ShimSkiaSharp.SKColor s_transparentBlack = new(0, 0, 0, 255);

        public ShimSkiaSharp.SKPicture? Picture { get; }

        public double? Opacity { get; set; }

        public SkiaSharp.SKMatrix? Transform { get;  set; }

        public SkiaSharp.SKPath? ClipGeometry { get; set; }

        public Brush? OpacityMask { get; set; }

        public List<Drawing> Children { get; } = new();

        public DrawingGroup(ShimSkiaSharp.SKPicture? picture = null, ResourceDictionary? resources = null, string? key = null)
        {
            Key = key;
            Picture = picture;
            Initialize(Picture, resources);
        }

        private void Initialize(ShimSkiaSharp.SKPicture? picture, ResourceDictionary? resources = null)
        {
            if (picture?.Commands is null)
            {
                return;
            }

            var totalMatrixStack = new Stack<SkiaSharp.SKMatrix?>();
            var clipPathStack = new Stack<SkiaSharp.SKPath?>();
            var paintStack = new Stack<ShimSkiaSharp.SKPaint?>();
            var layerStack = new Stack<DrawingGroup>();
            var currentTotalMatrix = default(SkiaSharp.SKMatrix?);
            var currentClipPath = default(SkiaSharp.SKPath?);

            layerStack.Push(this);

            foreach (var canvasCommand in picture.Commands)
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

                        var currentLayer = layerStack.Peek();
                        currentLayer.Children.Add(newLayer);
                        layerStack.Push(newLayer);
                        currentClipPath = path;

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

                        var currentLayer = layerStack.Peek();
                        currentLayer.Children.Add(newLayer);
                        layerStack.Push(newLayer);
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

                        var newLayer = new DrawingGroup
                        {
                            Transform = matrix
                        };

                        var currentLayer = layerStack.Peek();
                        currentLayer.Children.Add(newLayer);
                        layerStack.Push(newLayer);
                        currentTotalMatrix = matrix;
                        
                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(_, var skPaint):
                    {
                        if (skPaint is null)
                        {
                            break;
                        }

                        totalMatrixStack.Push(currentTotalMatrix);
                        currentTotalMatrix = default;

                        clipPathStack.Push(currentClipPath);
                        currentClipPath = default;

                        var newLayer = new DrawingGroup();
                        layerStack.Push(newLayer);
                        paintStack.Push(skPaint);

                        break;
                    }
                    case ShimSkiaSharp.SaveCanvasCommand(_):
                    {
                        totalMatrixStack.Push(currentTotalMatrix);
                        currentTotalMatrix = default;

                        clipPathStack.Push(currentClipPath);
                        currentClipPath = default;

                        paintStack.Push(default);

                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand(_):
                    {
                        if (paintStack.Count > 0)
                        {
                            var currentPaint = paintStack.Pop();
                            if (currentPaint is { })
                            {
                                var content = layerStack.Pop();
                                var currentLayer = layerStack.Peek();
                                var skPaint = currentPaint;

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
                        }

                        if (currentClipPath is { })
                        {
                            layerStack.Pop();
                        }
                        currentClipPath = default;
                        if (clipPathStack.Count > 0)
                        {
                            currentClipPath = clipPathStack.Pop();
                        }

                        if (currentTotalMatrix is { })
                        {
                            layerStack.Pop();
                        }
                        currentTotalMatrix = default;
                        if (totalMatrixStack.Count > 0)
                        {
                            currentTotalMatrix = totalMatrixStack.Pop();
                        }

                        break;
                    }
                    case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        var geometryDrawing = new GeometryDrawing(skPaint, path, resources);
 
                        var currentLayer = layerStack.Peek();
                        currentLayer.Children.Add(geometryDrawing);

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

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Center)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width / 2f, 0f));
                        }

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Right)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width, 0f));
                        }

                        var geometryDrawing = new GeometryDrawing(skPaint, path, resources);

                        var currentLayer = layerStack.Peek();
                        currentLayer.Children.Add(geometryDrawing);

                        break;
                    }
                    // ReSharper disable UnusedVariable
                    case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                    {
                        // TODO:
                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:
                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:
                        break;
                    }
                    // ReSharper restore  UnusedVariable
                }
            }
        }
    }
}
