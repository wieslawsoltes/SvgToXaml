using System;
using System.Text;

namespace SvgToXamlConverter
{
    public record LinearGradientBrush : GradientBrush
    {
        public SkiaSharp.SKPoint Start { get; init; }

        public SkiaSharp.SKPoint End { get; init; }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            var start = Start;
            var end = End;

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix =LocalMatrix.Value;
                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - Bounds.Location.Y);

                    start = localMatrix.MapPoint(start);
                    end = localMatrix.MapPoint(end);
                }
                else
                {
                    start.X = Math.Max(0f, start.X - Bounds.Location.X);
                    start.Y = Math.Max(0f, start.Y - Bounds.Location.Y);
                    end.X = Math.Max(0f, end.X - Bounds.Location.X);
                    end.Y = Math.Max(0f, end.Y - Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (LocalMatrix is null)
                    {
                        start.X = Math.Max(0f, start.X - Bounds.Location.X);
                        start.Y = Math.Max(0f, start.Y - Bounds.Location.Y);
                        end.X = Math.Max(0f, end.X - Bounds.Location.X);
                        end.Y = Math.Max(0f, end.Y - Bounds.Location.Y);
                    }
                }
            }

            sb.Append($"<LinearGradientBrush{XamlConverter.ToKey(Key)}");

            sb.Append($" StartPoint=\"{XamlConverter.ToPoint(start)}\"");
            sb.Append($" EndPoint=\"{XamlConverter.ToPoint(end)}\"");

            if (Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{XamlConverter.ToGradientSpreadMethod(Mode)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on LinearGradientBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <LinearGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </LinearGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (GradientStops.Count > 0)
            {
                sb.Append($"  <LinearGradientBrush.GradientStops>{context.NewLine}");

                for (var i = 0; i < GradientStops.Count; i++)
                {
                    var color = XamlConverter.ToHexColor(GradientStops[i].Color);
                    var offset = XamlConverter.ToString(GradientStops[i].Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </LinearGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</LinearGradientBrush>{context.NewLine}");

            return sb.ToString();
        }
    }
}
