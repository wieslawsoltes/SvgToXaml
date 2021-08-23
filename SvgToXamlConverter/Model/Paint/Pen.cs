using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvgToXamlConverter
{
    public record Pen : Resource
    {
        public SkiaSharp.SKRect Bounds { get; init; }

        public Brush? Brush { get; init; }

        public float StrokeWidth { get; init; }

        public ShimSkiaSharp.SKStrokeCap StrokeCap { get; init; }

        public ShimSkiaSharp.SKStrokeJoin StrokeJoin { get; init; }

        public float StrokeMiter { get; init; }

        public Dashes? Dashes { get; init; }

        public override string Generate(GeneratorContext context)
        {
            if (Brush is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<Pen{XamlConverter.ToKey(Key)}");

            if (Brush is SolidColorBrush solidColorBrush)
            {
                sb.Append($" Brush=\"{XamlConverter.ToHexColor(solidColorBrush.Color)}\"");
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (StrokeWidth != 1.0)
            {
                sb.Append($" Thickness=\"{XamlConverter.ToString(StrokeWidth)}\"");
            }

            if (StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
            {
                if (context.UseCompatMode)
                {
                    sb.Append($" StartLineCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                    sb.Append($" EndLineCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                }
                else
                {
                    sb.Append($" LineCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                }
            }

            if (context.UseCompatMode)
            {
                if (Dashes is { Intervals: { } })
                {
                    if (StrokeCap != ShimSkiaSharp.SKStrokeCap.Square)
                    {
                        sb.Append($" DashCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                    }
                }

                if (StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Miter)
                {
                    sb.Append($" LineJoin=\"{XamlConverter.ToPenLineJoin(StrokeJoin)}\"");
                }
            }
            else
            {
                if (StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
                {
                    sb.Append($" LineJoin=\"{XamlConverter.ToPenLineJoin(StrokeJoin)}\"");
                }
            }

            if (context.UseCompatMode)
            {
                var miterLimit = StrokeMiter;
                var strokeWidth = StrokeWidth;

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
                    sb.Append($" MiterLimit=\"{XamlConverter.ToString(miterLimit)}\"");
                }
            }
            else
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (StrokeMiter != 10.0)
                {
                    sb.Append($" MiterLimit=\"{XamlConverter.ToString(StrokeMiter)}\"");
                }
            }

            if (Brush is not SolidColorBrush || Dashes is { Intervals: { } })
            {
                sb.Append($">{context.NewLine}");
            }
            else
            {
                sb.Append($"/>{context.NewLine}");
            }

            if (Dashes is { Intervals: { } })
            {
                var dashes = new List<double>();

                foreach (var interval in Dashes.Intervals)
                {
                    dashes.Add(interval / StrokeWidth);
                }

                var offset = Dashes.Phase / StrokeWidth;

                sb.Append($"  <Pen.DashStyle>{context.NewLine}");
                sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(XamlConverter.ToString))}\" Offset=\"{XamlConverter.ToString(offset)}\"/>{context.NewLine}");
                sb.Append($"  </Pen.DashStyle>{context.NewLine}");
            }

            if (Brush is not SolidColorBrush)
            {
                sb.Append($"  <Pen.Brush>{context.NewLine}");
                sb.Append(Brush.Generate(context));
                sb.Append($"  </Pen.Brush>{context.NewLine}");
            }

            if (Brush is not SolidColorBrush || Dashes is { Intervals: { } })
            {
                sb.Append($"</Pen>{context.NewLine}");
            }

            return sb.ToString();
        }
    }
}
