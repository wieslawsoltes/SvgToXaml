using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SvgToXaml.Model.Containers;
using SvgToXaml.Model.Drawing;
using SvgToXaml.Model.Paint;

namespace SvgToXaml.Converter;

public static class PenGenerator
{
    public static string GeneratePen(Pen pen, XamlGeneratorSettings settings, Func<Image, XamlGeneratorSettings, SkiaSharp.SKMatrix?, string> generateImage)
    {
        if (pen.Brush is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        sb.Append($"<Pen{GeneratorHelper.ToKey(pen.Key)}");

        if (pen.Brush is SolidColorBrush solidColorBrush)
        {
            sb.Append($" Brush=\"{GeneratorHelper.ToHexColor(solidColorBrush.Color)}\"");
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (pen.StrokeWidth != 1.0)
        {
            sb.Append($" Thickness=\"{GeneratorHelper.ToXamlString(pen.StrokeWidth)}\"");
        }

        if (pen.StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
        {
            if (settings.UseCompatMode)
            {
                sb.Append($" StartLineCap=\"{GeneratorHelper.ToPenLineCap(pen.StrokeCap)}\"");
                sb.Append($" EndLineCap=\"{GeneratorHelper.ToPenLineCap(pen.StrokeCap)}\"");
            }
            else
            {
                sb.Append($" LineCap=\"{GeneratorHelper.ToPenLineCap(pen.StrokeCap)}\"");
            }
        }

        if (settings.UseCompatMode)
        {
            if (pen.Dashes is { Intervals: { } })
            {
                if (pen.StrokeCap != ShimSkiaSharp.SKStrokeCap.Square)
                {
                    sb.Append($" DashCap=\"{GeneratorHelper.ToPenLineCap(pen.StrokeCap)}\"");
                }
            }

            if (pen.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Miter)
            {
                sb.Append($" LineJoin=\"{GeneratorHelper.ToPenLineJoin(pen.StrokeJoin)}\"");
            }
        }
        else
        {
            if (pen.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
            {
                sb.Append($" LineJoin=\"{GeneratorHelper.ToPenLineJoin(pen.StrokeJoin)}\"");
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

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (miterLimit != 10.0)
            {
                sb.Append($" MiterLimit=\"{GeneratorHelper.ToXamlString(miterLimit)}\"");
            }
        }
        else
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (pen.StrokeMiter != 10.0)
            {
                sb.Append($" MiterLimit=\"{GeneratorHelper.ToXamlString(pen.StrokeMiter)}\"");
            }
        }

        if (pen.Brush is not SolidColorBrush || pen.Dashes is { Intervals: { } })
        {
            sb.Append($">{settings.NewLine}");
        }
        else
        {
            sb.Append($"/>{settings.NewLine}");
        }

        if (pen.Dashes is { Intervals: { } })
        {
            var dashes = new List<double>();

            foreach (var interval in pen.Dashes.Intervals)
            {
                dashes.Add(interval / pen.StrokeWidth);
            }

            var offset = pen.Dashes.Phase / pen.StrokeWidth;

            sb.Append($"  <Pen.DashStyle>{settings.NewLine}");
            sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(GeneratorHelper.ToXamlString))}\" Offset=\"{GeneratorHelper.ToXamlString(offset)}\"/>{settings.NewLine}");
            sb.Append($"  </Pen.DashStyle>{settings.NewLine}");
        }

        if (pen.Brush is not SolidColorBrush)
        {
            sb.Append($"  <Pen.Brush>{settings.NewLine}");
            sb.Append(BrushGenerator.GenerateBrush(pen.Brush, settings, generateImage));
            sb.Append($"  </Pen.Brush>{settings.NewLine}");
        }

        if (pen.Brush is not SolidColorBrush || pen.Dashes is { Intervals: { } })
        {
            sb.Append($"</Pen>{settings.NewLine}");
        }

        return sb.ToString();
    }
}
