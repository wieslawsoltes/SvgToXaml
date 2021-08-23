using System;
using System.Text;

namespace SvgToXamlConverter
{
    public record TwoPointConicalGradientBrush : GradientBrush
    {
        public float StartRadius { get; init; }

        public float EndRadius { get; init; }

        public SkiaSharp.SKPoint Start { get; init; }

        public SkiaSharp.SKPoint End { get; init; }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
            // ReSharper disable once UnusedVariable
            var startRadius = StartRadius;

            // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
            // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
            var endRadius = EndRadius;

            var center = End;
            var gradientOrigin = Start;

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix = LocalMatrix.Value;

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - Bounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radiusMapped = localMatrix.MapVector(new SkiaSharp.SKPoint(endRadius, 0));
                    endRadius = radiusMapped.X;
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
                endRadius = endRadius / Bounds.Width;
            }

            sb.Append($"<RadialGradientBrush{XamlConverter.ToKey(Key)}");

            sb.Append($" Center=\"{XamlConverter.ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{XamlConverter.ToPoint(gradientOrigin)}\"");

            if (context.UseCompatMode)
            {
                sb.Append($" RadiusX=\"{XamlConverter.ToString(endRadius)}\"");
                sb.Append($" RadiusY=\"{XamlConverter.ToString(endRadius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{XamlConverter.ToString(endRadius)}\"");
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
