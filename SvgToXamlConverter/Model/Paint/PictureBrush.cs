using System.Text;

namespace SvgToXamlConverter
{
    public record PictureBrush : GradientBrush
    {
        public Image? Picture { get; init; }

        public ShimSkiaSharp.SKRect CullRect { get; init; }

        public ShimSkiaSharp.SKRect Tile { get; init; }

        public ShimSkiaSharp.SKShaderTileMode TileMode { get; init; }
 
        public override string Generate(GeneratorContext context)
        {
            if (Picture is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix = LocalMatrix.Value;

                    if (!localMatrix.IsIdentity)
                    {
#if DEBUG
                        sb.Append($"<!-- TODO: Transform: {XamlConverter.ToMatrix(localMatrix)} -->{context.NewLine}");
#endif
                    }
                }
                else
                {
                    // TODO: Adjust using Bounds.Location ?
                }
            }

            var sourceRect = CullRect;
            var destinationRect = Tile;

            // TODO: Use different visual then Image ?
            sb.Append($"<VisualBrush{XamlConverter.ToKey(Key)}");

            if (TileMode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" TileMode=\"{XamlConverter.ToTileMode(TileMode)}\"");
            }

            if (context.UseCompatMode)
            {
                if (!sourceRect.IsEmpty)
                {
                    sb.Append($" Viewport=\"{XamlConverter.ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                }

                if (!destinationRect.IsEmpty)
                {
                    sb.Append($" Viewbox=\"{XamlConverter.ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
                }
            }
            else
            {
                if (!sourceRect.IsEmpty)
                {
                    sb.Append($" SourceRect=\"{XamlConverter.ToRect(sourceRect)}\"");
                }

                if (!destinationRect.IsEmpty)
                {
                    sb.Append($" DestinationRect=\"{XamlConverter.ToRect(destinationRect)}\"");
                }
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on VisualBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <VisualBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </VisualBrush.Transform>{context.NewLine}");
                }
            }

            if (Picture is not null)
            {
                sb.Append($"  <VisualBrush.Visual>{context.NewLine}");
                sb.Append(Picture.Generate(context with { WriteResources = false }));
                sb.Append($"{context.NewLine}");
                sb.Append($"  </VisualBrush.Visual>{context.NewLine}");
            }

            sb.Append($"</VisualBrush>{context.NewLine}");

            return sb.ToString();
        }
    }
}
