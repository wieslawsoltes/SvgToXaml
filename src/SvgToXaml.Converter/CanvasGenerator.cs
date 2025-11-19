using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SvgToXaml.Model.Containers;
using SvgToXaml.Model.Drawing;
using SvgToXaml.Model.Paint;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Converter;

public class CanvasGenerator
{
    private static string ToKey(string? key)
    {
        return GeneratorHelper.ToKey(key);
    }

    private static string ToGradientSpreadMethod(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
    {
        return GeneratorHelper.ToGradientSpreadMethod(shaderTileMode);
    }

    private static string ToTileMode(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
    {
        return GeneratorHelper.ToTileMode(shaderTileMode);
    }

    private static string ToPenLineCap(ShimSkiaSharp.SKStrokeCap strokeCap)
    {
        return GeneratorHelper.ToPenLineCap(strokeCap);
    }

    private static string ToPenLineJoin(ShimSkiaSharp.SKStrokeJoin strokeJoin, bool useCompatMode = false)
    {
        return GeneratorHelper.ToPenLineJoin(strokeJoin, useCompatMode);
    }

    private static string ToXamlString(double value)
    {
        return GeneratorHelper.ToXamlString(value);
    }

    private static string ToXamlString(float value)
    {
        return GeneratorHelper.ToXamlString(value);
    }

    private static string ToHexColor(ShimSkiaSharp.SKColor skColor)
    {
        return GeneratorHelper.ToHexColor(skColor);
    }

    private static string ToPoint(SkiaSharp.SKPoint skPoint)
    {
        return GeneratorHelper.ToPoint(skPoint);
    }

    private static string ToRect(ShimSkiaSharp.SKRect sKRect)
    {
        return GeneratorHelper.ToRect(sKRect);
    }

    private static string ToRect(SkiaSharp.SKRect sKRect)
    {
        return GeneratorHelper.ToRect(sKRect);
    }

    private static string ToMatrix(SkiaSharp.SKMatrix skMatrix)
    {
        return GeneratorHelper.ToMatrix(skMatrix);
    }

    private static string ToSvgPathData(SkiaSharp.SKPath path, SkiaSharp.SKMatrix matrix)
    {
        return GeneratorHelper.ToSvgPathData(path, matrix);
    }

    private string GenerateBrush(Brush brush, XamlGeneratorSettings settings)
    {
        return BrushGenerator.GenerateBrush(brush, settings, GenerateImage);
    }

    private string GeneratePen(Pen pen, XamlGeneratorSettings settings)
    {
        return PenGenerator.GeneratePen(pen, settings, GenerateImage);
    }

    private string GenerateResource(Resource resource, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        return resource switch
        {
            SolidColorBrush solidColorBrush => BrushGenerator.GenerateSolidColorBrush(solidColorBrush, settings),
            LinearGradientBrush linearGradientBrush => BrushGenerator.GenerateLinearGradientBrush(linearGradientBrush, settings),
            RadialGradientBrush radialGradientBrush => BrushGenerator.GenerateRadialGradientBrush(radialGradientBrush, settings),
            TwoPointConicalGradientBrush twoPointConicalGradientBrush => BrushGenerator.GenerateTwoPointConicalGradientBrush(twoPointConicalGradientBrush, settings),
            PictureBrush pictureBrush => BrushGenerator.GeneratePictureBrush(pictureBrush, settings, GenerateImage),
            Pen pen => GeneratePen(pen, settings),
            GeometryDrawing geometryDrawing => GenerateGeometryDrawing(geometryDrawing, settings, matrix),
            DrawingGroup drawingGroup => GenerateDrawingGroup(drawingGroup, settings),
            DrawingImage drawingImage => GenerateDrawingImage(drawingImage, settings, matrix),
            ImageDrawing imageDrawing => GenerateImageDrawing(imageDrawing, settings, matrix),
            GlyphRunDrawing glyphRunDrawing => GenerateGlyphRunDrawing(glyphRunDrawing, settings, matrix),
            Image image => GenerateImage(image, settings, matrix),
            _ => ""
        };
    }

    private string GenerateResourceDictionary(ResourceDictionary resourceDictionary, XamlGeneratorSettings settings)
    {
        return ResourceGenerator.GenerateResourceDictionary(resourceDictionary, settings, GenerateImage);
    }

    public string GenerateStyles(Styles styles, XamlGeneratorSettings settings)
    {
        return ResourceGenerator.GenerateStyles(styles, settings, GenerateResource, GenerateResourceDictionary);
    }

    public string GenerateImage(Image image, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (image.Source is null)
        {
            return "";
        }
        return GenerateDrawingImage(image.Source, settings, matrix);
    }

    public string Generate(Drawing drawing, XamlGeneratorSettings settings)
    {
        return GenerateDrawing(drawing, settings, null);
    }

    private string GenerateDrawing(Drawing drawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        return drawing switch
        {
            GeometryDrawing geometryDrawing => GenerateGeometryDrawing(geometryDrawing, settings, matrix),
            DrawingGroup drawingGroup => GenerateDrawingGroup(drawingGroup, settings),
            DrawingImage drawingImage => GenerateDrawingImage(drawingImage, settings, matrix),
            ImageDrawing imageDrawing => GenerateImageDrawing(imageDrawing, settings, matrix),
            GlyphRunDrawing glyphRunDrawing => GenerateGlyphRunDrawing(glyphRunDrawing, settings, matrix),
            _ => ""
        };
    }

    private string GenerateImageDrawing(ImageDrawing imageDrawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        var sb = new StringBuilder();
        sb.Append($"<Image{ToKey(imageDrawing.Key)}");
        
        sb.Append($" Canvas.Left=\"{ToXamlString(imageDrawing.Rect.Left)}\"");
        sb.Append($" Canvas.Top=\"{ToXamlString(imageDrawing.Rect.Top)}\"");
        sb.Append($" Width=\"{ToXamlString(imageDrawing.Rect.Width)}\"");
        sb.Append($" Height=\"{ToXamlString(imageDrawing.Rect.Height)}\"");

        if (imageDrawing.ImageSource is { })
        {
             sb.Append($" Source=\"{imageDrawing.ImageSource}\"");
        }
        sb.Append($"/>{settings.NewLine}");
        return sb.ToString();
    }

    private string GenerateGlyphRunDrawing(GlyphRunDrawing glyphRunDrawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        // TODO: Implement GlyphRunDrawing for Canvas
        return "";
    }

    private string GenerateGeometryDrawing(GeometryDrawing geometryDrawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (geometryDrawing.Paint is null || geometryDrawing.Geometry is null)
        {
            return "";
        }
 
        var sb = new StringBuilder();
            
        sb.Append($"<Path{ToKey(geometryDrawing.Key)}");

        var isFilled = geometryDrawing.Brush is { };
        var isStroked = geometryDrawing.Pen is { };

        if (isFilled && geometryDrawing.Brush is SolidColorBrush solidColorBrush && settings.Resources is null)
        {
            sb.Append($" Fill=\"{ToHexColor(solidColorBrush.Color)}\"");
        }

        var brush = default(Brush);
        var pen = default(Pen);

        if (isFilled && geometryDrawing.Brush is { } and not SolidColorBrush && settings.Resources is null)
        {
            brush = geometryDrawing.Brush;
        }

        if (isFilled && geometryDrawing.Paint is { } && settings.Resources is { })
        {
            bool haveBrush = false;

            if (settings.ReuseExistingResources)
            {
                var existingBrush = settings.Resources.Brushes.FirstOrDefault(x =>
                {
                    if (x.Value.Paint.Shader is { } && x.Value.Paint.Shader.Equals(geometryDrawing.Paint.Shader))
                    {
                        return true;
                    }
                    else if (x.Value.Paint.Shader is null && geometryDrawing.Paint.Shader is null && x.Value.Paint.Color.Equals(geometryDrawing.Paint.Color))
                    {
                        return true;
                    }

                    return false;
                });

                if (!string.IsNullOrEmpty(existingBrush.Key))
                {
                    settings.Resources.UseBrushes.Add(existingBrush.Value.Brush);
                    sb.Append($" Fill=\"{{DynamicResource {existingBrush.Key}}}\"");
                    haveBrush = true;
                }
            }

            if (!haveBrush)
            {
                if (geometryDrawing.Brush is { } && settings.Resources is { } && geometryDrawing.Brush.Key is { })
                {
                    settings.Resources.UseBrushes.Add(geometryDrawing.Brush);
                    sb.Append($" Fill=\"{{DynamicResource {geometryDrawing.Brush.Key}}}\"");
                    haveBrush = true;
                }
                    
                if (!haveBrush)
                {
                    brush = geometryDrawing.Brush;
                }
            }
        }

        if (isStroked && geometryDrawing.Pen is { } && settings.Resources is null)
        {
            pen = geometryDrawing.Pen;
        }

        if (isStroked && geometryDrawing.Paint is { } && settings.Resources is { })
        {
            bool havePen = false;

            if (settings.ReuseExistingResources)
            {
                var existingPen = settings.Resources.Pens.FirstOrDefault(x =>
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
                    else if (x.Value.Paint.Shader is null && geometryDrawing.Paint.Shader is null 
                        && x.Value.Paint.Color.Equals(geometryDrawing.Paint.Color)
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
                    settings.Resources.UsePens.Add(existingPen.Value.Pen);
                    // Path doesn't have a "Pen" property, it has Stroke, StrokeThickness, etc.
                    // If we are reusing a Pen resource, we can't easily apply it to a Path unless we use a Style or multiple properties.
                    // But Pen resource in XAML is usually for GeometryDrawing.
                    // For Path, we usually have Brush resources.
                    // If the user wants Canvas/Path, they probably expect Stroke="..." StrokeThickness="..."
                    // So reusing "Pen" resources might not work directly on Path.
                    // We might need to decompose the Pen.
                    // But if the resource is a Pen, we can't assign it to Stroke (which expects a Brush).
                    // So for CanvasGenerator, we should probably only reuse Brushes for Stroke.
                    // And set StrokeThickness etc. manually.
                    // Or maybe we shouldn't reuse Pen resources here.
                    // Let's assume for now we don't reuse Pen resources for Path, but we can reuse Brush resources for Stroke.
                    
                    // But wait, the existing code in XamlGenerator reuses Pens.
                    // If we are generating Path, we can't use Pen property.
                    // So we should probably ignore Pen reuse for now and just generate properties.
                    // Or we can reuse the Brush from the Pen if available.
                }
            }

            if (!havePen)
            {
                // ...
                pen = geometryDrawing.Pen;
            }
        }

        if (geometryDrawing.Geometry is { })
        {
            sb.Append($" Data=\"{ToSvgPathData(geometryDrawing.Geometry, matrix ?? SkiaSharp.SKMatrix.CreateIdentity())}\"");
        }

        // Handle Pen properties on Path
        if (pen is { })
        {
            if (pen.Brush is SolidColorBrush solidColorBrushPen)
            {
                sb.Append($" Stroke=\"{ToHexColor(solidColorBrushPen.Color)}\"");
            }
            else if (pen.Brush is { })
            {
                // Complex brush for stroke
                // We will handle it in property element syntax if needed, or if it's a resource
            }

            if (pen.StrokeWidth != 1.0)
            {
                sb.Append($" StrokeThickness=\"{ToXamlString(pen.StrokeWidth)}\"");
            }

            if (pen.StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
            {
                if (settings.UseCompatMode)
                {
                    sb.Append($" StrokeStartLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                    sb.Append($" StrokeEndLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                }
                else
                {
                    sb.Append($" StrokeLineCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                }
            }

            if (settings.UseCompatMode)
            {
                if (pen.Dashes is { Intervals: { } })
                {
                    if (pen.StrokeCap != ShimSkiaSharp.SKStrokeCap.Square)
                    {
                        sb.Append($" StrokeDashCap=\"{ToPenLineCap(pen.StrokeCap)}\"");
                    }
                }

                if (pen.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Miter)
                {
                    sb.Append($" StrokeLineJoin=\"{ToPenLineJoin(pen.StrokeJoin)}\"");
                }
            }
            else
            {
                if (pen.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
                {
                    sb.Append($" StrokeJoin=\"{ToPenLineJoin(pen.StrokeJoin)}\"");
                }
            }
            
            if (settings.UseCompatMode)
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

                if (miterLimit != 10.0)
                {
                    sb.Append($" StrokeMiterLimit=\"{ToXamlString(miterLimit)}\"");
                }
            }
            else
            {
            }
            
            if (pen.Dashes is { Intervals: { } })
            {
                var dashes = new List<double>();

                foreach (var interval in pen.Dashes.Intervals)
                {
                    dashes.Add(interval / pen.StrokeWidth);
                }

                var offset = pen.Dashes.Phase / pen.StrokeWidth;

                sb.Append($" StrokeDashArray=\"{string.Join(",", dashes.Select(ToXamlString))}\" StrokeDashOffset=\"{ToXamlString(offset)}\"");
            }
        }

        if (brush is { } || (pen is { } && pen.Brush is not SolidColorBrush))
        {
            sb.Append($">{settings.NewLine}");
        }
        else
        {
            sb.Append($"/>{settings.NewLine}");
        }

        if (brush is { })
        {
            sb.Append($"  <Path.Fill>{settings.NewLine}");
            sb.Append(GenerateBrush(brush, settings));
            sb.Append($"  </Path.Fill>{settings.NewLine}");
        }

        if (pen is { } && pen.Brush is { } and not SolidColorBrush)
        {
            sb.Append($"  <Path.Stroke>{settings.NewLine}");
            sb.Append(GenerateBrush(pen.Brush, settings));
            sb.Append($"  </Path.Stroke>{settings.NewLine}");
        }

        if (brush is { } || (pen is { } && pen.Brush is { } and not SolidColorBrush))
        {
            sb.Append($"</Path>{settings.NewLine}");
        }

        return sb.ToString();
    }

    public string GenerateDrawingGroup(DrawingGroup drawingGroup, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        sb.Append($"<Canvas{ToKey(drawingGroup.Key)}");

        if (drawingGroup.Opacity is { })
        {
            sb.Append($" Opacity=\"{ToXamlString(drawingGroup.Opacity.Value)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (drawingGroup.ClipGeometry is { })
        {
            var clipGeometry = ToSvgPathData(drawingGroup.ClipGeometry, SkiaSharp.SKMatrix.CreateIdentity());

            sb.Append($"  <Canvas.Clip>{settings.NewLine}");
            sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{settings.NewLine}");
            sb.Append($"  </Canvas.Clip>{settings.NewLine}");
        }

        if (drawingGroup.Transform is { } && !settings.TransformGeometry)
        {
            var matrix = drawingGroup.Transform.Value;

            sb.Append($"  <Canvas.RenderTransform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{settings.NewLine}");
            sb.Append($"  </Canvas.RenderTransform>{settings.NewLine}");
        }

        if (drawingGroup.OpacityMask is { })
        {
            sb.Append($"<Canvas.OpacityMask>{settings.NewLine}");
            sb.Append(GenerateBrush(drawingGroup.OpacityMask, settings));
            sb.Append($"</Canvas.OpacityMask>{settings.NewLine}");
        }

        // TODO: Handle Picture (transparent background) if needed
        // For Canvas, we can just add a Rectangle with transparent fill?
        if (settings.AddTransparentBackground && drawingGroup.Picture is { })
        {
            var left = drawingGroup.Picture.CullRect.Left;
            var top = drawingGroup.Picture.CullRect.Top;
            var width = drawingGroup.Picture.CullRect.Width;
            var height = drawingGroup.Picture.CullRect.Height;
            
            sb.Append($"<Rectangle Canvas.Left=\"{ToXamlString(left)}\" Canvas.Top=\"{ToXamlString(top)}\" Width=\"{ToXamlString(width)}\" Height=\"{ToXamlString(height)}\" Fill=\"Transparent\" />{settings.NewLine}");
        }

        foreach (var child in drawingGroup.Children)
        {
            sb.Append(GenerateDrawing(child, settings, settings.TransformGeometry ? drawingGroup.Transform : null));
        }

        sb.Append($"</Canvas>");

        return sb.ToString();
    }

    private string GenerateDrawingImage(DrawingImage drawingImage, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (drawingImage.Drawing is null)
        {
            return "";
        }

        // DrawingImage is usually a wrapper. If we are generating Canvas, we just unwrap it.
        return GenerateDrawing(drawingImage.Drawing, settings, matrix);
    }
}
