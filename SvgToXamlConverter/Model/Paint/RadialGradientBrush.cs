using System;
using System.Text;

namespace SvgToXamlConverter
{
    public record RadialGradientBrush : GradientBrush
    {
        public SkiaSharp.SKPoint Center { get; init; }

        public float Radius { get; init; }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            var radius = Radius;

            var center = Center;
            var gradientOrigin = Center;

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix = LocalMatrix.Value;

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - Bounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radiusMapped = localMatrix.MapVector(new SkiaSharp.SKPoint(radius, 0));
                    radius = radiusMapped.X;
                }
                else
                {
                    center.X = Math.Max(0f, center.X - Bounds.Location.X);
                    center.Y = Math.Max(0f, center.Y - Bounds.Location.Y);
                    gradientOrigin.X = Math.Max(0f, gradientOrigin.X - Bounds.Location.X);
                    gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (LocalMatrix is null)
                    {
                        center.X = Math.Max(0f, center.X - Bounds.Location.X);
                        center.Y = Math.Max(0f, center.Y - Bounds.Location.Y);
                        gradientOrigin.X = Math.Max(0f, gradientOrigin.X - Bounds.Location.X);
                        gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - Bounds.Location.Y);
                    }
                }
            }

            if (!context.UseCompatMode)
            {
                radius = radius / Bounds.Width;
            }

            sb.Append($"<RadialGradientBrush{XamlConverter.ToKey(Key)}");

            sb.Append($" Center=\"{XamlConverter.ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{XamlConverter.ToPoint(gradientOrigin)}\"");

            if (context.UseCompatMode)
            {
                sb.Append($" RadiusX=\"{XamlConverter.ToString(radius)}\"");
                sb.Append($" RadiusY=\"{XamlConverter.ToString(radius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{XamlConverter.ToString(radius)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            if (Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{XamlConverter.ToGradientSpreadMethod(Mode)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on RadialGradientBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <RadialGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </RadialGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (GradientStops.Count > 0)
            {
                sb.Append($"  <RadialGradientBrush.GradientStops>{context.NewLine}");

                for (var i = 0; i < GradientStops.Count; i++)
                {
                    var color = XamlConverter.ToHexColor(GradientStops[i].Color);
                    var offset = XamlConverter.ToString(GradientStops[i].Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </RadialGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</RadialGradientBrush>{context.NewLine}");

            return sb.ToString();
        }
    }
}
