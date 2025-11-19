using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SvgToXaml.Model.Containers;
using SvgToXaml.Model.Drawing;
using SvgToXaml.Model.Paint;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Converter;

public class XamlGenerator
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
        return BrushGenerator.GenerateBrush(brush, settings, (image, s, m) => GenerateImage(image, s with { WriteResources = false, AddTransparentBackground = false }, m));
    }

    private string GeneratePen(Pen pen, XamlGeneratorSettings settings)
    {
        return PenGenerator.GeneratePen(pen, settings, (image, s, m) => GenerateImage(image, s with { WriteResources = false, AddTransparentBackground = false }, m));
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
        sb.Append($"<ImageDrawing{ToKey(imageDrawing.Key)}");
        sb.Append($" Rect=\"{ToRect(imageDrawing.Rect)}\"");
        if (imageDrawing.ImageSource is { })
        {
             sb.Append($" ImageSource=\"{imageDrawing.ImageSource}\"");
        }
        sb.Append($"/>{settings.NewLine}");
        return sb.ToString();
    }

    private string GenerateGlyphRunDrawing(GlyphRunDrawing glyphRunDrawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        var sb = new StringBuilder();
        sb.Append($"<GlyphRunDrawing{ToKey(glyphRunDrawing.Key)}");
        
        if (glyphRunDrawing.Foreground is SolidColorBrush solidColorBrush)
        {
             sb.Append($" Foreground=\"{ToHexColor(solidColorBrush.Color)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (glyphRunDrawing.Foreground is not SolidColorBrush && glyphRunDrawing.Foreground is { })
        {
            sb.Append($"  <GlyphRunDrawing.Foreground>{settings.NewLine}");
            sb.Append(GenerateBrush(glyphRunDrawing.Foreground, settings));
            sb.Append($"  </GlyphRunDrawing.Foreground>{settings.NewLine}");
        }

        if (glyphRunDrawing.GlyphRun is { })
        {
            sb.Append($"  <GlyphRunDrawing.GlyphRun>{settings.NewLine}");
            sb.Append(GenerateGlyphRun(glyphRunDrawing.GlyphRun, settings));
            sb.Append($"  </GlyphRunDrawing.GlyphRun>{settings.NewLine}");
        }

        sb.Append($"</GlyphRunDrawing>{settings.NewLine}");
        return sb.ToString();
    }

    private string GenerateGlyphRun(GlyphRun glyphRun, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();
        sb.Append($"<GlyphRun BaselineOrigin=\"{ToPoint(glyphRun.BaselineOrigin)}\"");

        if (glyphRun.GlyphTypeface is { })
        {
            sb.Append($" GlyphTypeface=\"{glyphRun.GlyphTypeface}\"");
        }

        if (glyphRun.FontRenderingEmSize > 0)
        {
            sb.Append($" FontRenderingEmSize=\"{ToXamlString(glyphRun.FontRenderingEmSize)}\"");
        }

        if (glyphRun.GlyphIndices is { })
        {
            sb.Append($" GlyphIndices=\"{string.Join(",", glyphRun.GlyphIndices)}\"");
        }

        if (glyphRun.AdvanceWidths is { })
        {
            sb.Append($" AdvanceWidths=\"{string.Join(",", glyphRun.AdvanceWidths.Select(ToXamlString))}\"");
        }

        if (glyphRun.GlyphOffsets is { })
        {
            sb.Append($" GlyphOffsets=\"{string.Join(",", glyphRun.GlyphOffsets.Select(ToPoint))}\"");
        }

        if (glyphRun.Characters is { })
        {
            sb.Append($" Characters=\"{new string(glyphRun.Characters)}\"");
        }

        if (glyphRun.BidiLevel > 0)
        {
            sb.Append($" BidiLevel=\"{glyphRun.BidiLevel}\"");
        }

        if (glyphRun.Language is { })
        {
            sb.Append($" Language=\"{glyphRun.Language}\"");
        }

        if (glyphRun.DeviceFontName is { })
        {
            sb.Append($" DeviceFontName=\"{glyphRun.DeviceFontName}\"");
        }

        if (glyphRun.IsSideways)
        {
            sb.Append($" IsSideways=\"True\"");
        }

        if (glyphRun.ClusterMap is { })
        {
            sb.Append($" ClusterMap=\"{string.Join(",", glyphRun.ClusterMap)}\"");
        }

        if (glyphRun.CaretStops is { })
        {
            sb.Append($" CaretStops=\"{string.Join(",", glyphRun.CaretStops)}\"");
        }

        sb.Append($"/>{settings.NewLine}");
        return sb.ToString();
    }

    private string GenerateGeometryDrawing(GeometryDrawing geometryDrawing, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (geometryDrawing.Paint is null || geometryDrawing.Geometry is null)
        {
            return "";
        }
 
        var sb = new StringBuilder();
            
        sb.Append($"<GeometryDrawing");

        var isFilled = geometryDrawing.Brush is { };
        var isStroked = geometryDrawing.Pen is { };

        if (isFilled && geometryDrawing.Brush is SolidColorBrush solidColorBrush && settings.Resources is null)
        {
            sb.Append($" Brush=\"{ToHexColor(solidColorBrush.Color)}\"");
        }

        var brush = default(Brush);
        var pen = default(Pen);

        if (isFilled && geometryDrawing.Brush is { } and not SolidColorBrush && settings.Resources is null)
        {
            brush =geometryDrawing. Brush;
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
                    sb.Append($" Brush=\"{{DynamicResource {existingBrush.Key}}}\"");
                    haveBrush = true;
                }
            }

            if (!haveBrush)
            {
                if (geometryDrawing.Brush is { } && settings.Resources is { } && geometryDrawing.Brush.Key is { })
                {
                    settings.Resources.UseBrushes.Add(geometryDrawing.Brush);
                    sb.Append($" Brush=\"{{DynamicResource {geometryDrawing.Brush.Key}}}\"");
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
                    sb.Append($" Pen=\"{{DynamicResource {existingPen.Key}}}\"");
                    havePen = true;
                }
            }

            if (!havePen)
            {
                if (geometryDrawing.Pen is { } && settings.Resources is { } && geometryDrawing.Pen.Key is { })
                {
                    settings.Resources.UsePens.Add(geometryDrawing.Pen);
                    sb.Append($" Pen=\"{{DynamicResource {geometryDrawing.Pen.Key}}}\"");
                    havePen = true;
                }

                if (!havePen)
                {
                    pen = geometryDrawing.Pen;
                }
            }
        }

        if (geometryDrawing.Geometry is { })
        {
            sb.Append($" Geometry=\"{ToSvgPathData(geometryDrawing.Geometry, matrix ?? SkiaSharp.SKMatrix.CreateIdentity())}\"");
        }

        if (brush is { } || pen is { })
        {
            sb.Append($">{settings.NewLine}");
        }
        else
        {
            sb.Append($"/>{settings.NewLine}");
        }

        if (brush is { })
        {
            sb.Append($"  <GeometryDrawing.Brush>{settings.NewLine}");
            sb.Append(GenerateBrush(brush, settings));
            sb.Append($"  </GeometryDrawing.Brush>{settings.NewLine}");
        }

        if (pen is { })
        {
            sb.Append($"  <GeometryDrawing.Pen>{settings.NewLine}");
            sb.Append(GeneratePen(pen, settings));
            sb.Append($"  </GeometryDrawing.Pen>{settings.NewLine}");
        }

        if (brush is { } || pen is { })
        {
            sb.Append($"</GeometryDrawing>{settings.NewLine}");
        }

        return sb.ToString();
    }

    public string GenerateDrawingGroup(DrawingGroup drawingGroup, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        sb.Append($"<DrawingGroup{ToKey(drawingGroup.Key)}");

        if (drawingGroup.Opacity is { })
        {
            sb.Append($" Opacity=\"{ToXamlString(drawingGroup.Opacity.Value)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (drawingGroup.ClipGeometry is { })
        {
            var clipGeometry = ToSvgPathData(drawingGroup.ClipGeometry, SkiaSharp.SKMatrix.CreateIdentity());

            sb.Append($"  <DrawingGroup.ClipGeometry>{settings.NewLine}");
            sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{settings.NewLine}");
            sb.Append($"  </DrawingGroup.ClipGeometry>{settings.NewLine}");
        }

        if (drawingGroup.Transform is { } && !settings.TransformGeometry)
        {
            var matrix = drawingGroup.Transform.Value;

            sb.Append($"  <DrawingGroup.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{settings.NewLine}");
            sb.Append($"  </DrawingGroup.Transform>{settings.NewLine}");
        }

        if (drawingGroup.OpacityMask is { })
        {
            sb.Append($"<DrawingGroup.OpacityMask>{settings.NewLine}");
            sb.Append(GenerateBrush(drawingGroup.OpacityMask, settings));
            sb.Append($"</DrawingGroup.OpacityMask>{settings.NewLine}");
        }

        if (settings.AddTransparentBackground && drawingGroup.Picture is { })
        {
            var left = drawingGroup.Picture.CullRect.Left;
            var top = drawingGroup.Picture.CullRect.Top;
            var right = drawingGroup.Picture.CullRect.Right;
            var bottom = drawingGroup.Picture.CullRect.Bottom;
            sb.Append($"<GeometryDrawing Brush=\"Transparent\" ");
            sb.Append($"Geometry=\"F1");
            sb.Append($"M{ToXamlString(left)},{ToXamlString(top)}");
            sb.Append($"L{ToXamlString(right)},{ToXamlString(top)}");
            sb.Append($"L{ToXamlString(right)},{ToXamlString(bottom)}");
            sb.Append($"L{ToXamlString(left)},{ToXamlString(bottom)}");
            sb.Append($"z\" ");
            sb.Append($"/>{settings.NewLine}");
        }

        foreach (var child in drawingGroup.Children)
        {
            sb.Append(GenerateDrawing(child, settings, settings.TransformGeometry ? drawingGroup.Transform : null));
        }

        sb.Append($"</DrawingGroup>");

        return sb.ToString();
    }

    private string GenerateDrawingImage(DrawingImage drawingImage, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (drawingImage.Drawing is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        sb.Append($"<DrawingImage>{settings.NewLine}");

        if (settings.UseCompatMode)
        {
            sb.Append($"  <DrawingImage.Drawing>{settings.NewLine}");
        }

        sb.Append(GenerateDrawing(drawingImage.Drawing, settings, matrix));

        if (settings.UseCompatMode)
        {
            sb.Append($"  </DrawingImage.Drawing>{settings.NewLine}");
        }

        sb.Append($"</DrawingImage>{settings.NewLine}");

        return sb.ToString();
    }

    private string GenerateResource(Resource resource, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        return resource switch
        {
            SolidColorBrush solidColorBrush => BrushGenerator.GenerateSolidColorBrush(solidColorBrush, settings),
            LinearGradientBrush linearGradientBrush => BrushGenerator.GenerateLinearGradientBrush(linearGradientBrush, settings),
            RadialGradientBrush radialGradientBrush => BrushGenerator.GenerateRadialGradientBrush(radialGradientBrush, settings),
            TwoPointConicalGradientBrush twoPointConicalGradientBrush => BrushGenerator.GenerateTwoPointConicalGradientBrush(twoPointConicalGradientBrush, settings),
            PictureBrush pictureBrush => BrushGenerator.GeneratePictureBrush(pictureBrush, settings, (image, s, m) => GenerateImage(image, s with { WriteResources = false, AddTransparentBackground = false }, m)),
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
        return ResourceGenerator.GenerateResourceDictionary(resourceDictionary, settings, (image, s, m) => GenerateImage(image, s with { WriteResources = false, AddTransparentBackground = false }, m));
    }

    public string GenerateImage(Image image, XamlGeneratorSettings settings, SkiaSharp.SKMatrix? matrix)
    {
        if (image.Source is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        var content = GenerateDrawingImage(image.Source, settings, matrix);

        sb.Append($"<Image{ToKey(image.Key)}");

        if (settings.Resources is { } && (settings.Resources.Brushes.Count > 0 || settings.Resources.Pens.Count > 0) && settings.WriteResources)
        {
            // sb.Append(settings.UseCompatMode
            //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
            //     : $" xmlns=\"https://github.com/avaloniaui\"");
            // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
        }

        sb.Append($">{settings.NewLine}");

        if (settings.Resources is { } && (settings.Resources.Brushes.Count > 0 || settings.Resources.Pens.Count > 0) && settings.WriteResources)
        {
            sb.Append($"<Image.Resources>{settings.NewLine}");
            sb.Append(GenerateResourceDictionary(settings.Resources, settings with { WriteResources = false, AddTransparentBackground = false }));
            sb.Append($"</Image.Resources>{settings.NewLine}");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($"<Image.Source>{settings.NewLine}");
        }

        sb.Append(content);

        if (settings.UseCompatMode)
        {
            sb.Append($"</Image.Source>{settings.NewLine}");
        }

        sb.Append($"</Image>");

        return sb.ToString();
    }

    public string GenerateStyles(Styles styles, XamlGeneratorSettings settings)
    {
        return ResourceGenerator.GenerateStyles(styles, settings, GenerateResource, GenerateResourceDictionary);
    }
}
