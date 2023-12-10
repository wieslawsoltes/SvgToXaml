using SvgToXaml.Model.Containers;

namespace SvgToXaml.Model.Paint;

public record PictureBrush : GradientBrush
{
    public Image? Picture { get; init; }

    public ShimSkiaSharp.SKRect CullRect { get; init; }

    public ShimSkiaSharp.SKRect Tile { get; init; }

    public ShimSkiaSharp.SKShaderTileMode TileMode { get; init; }
}
