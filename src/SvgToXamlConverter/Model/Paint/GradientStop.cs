using SvgToXamlConverter.Model.Resources;

namespace SvgToXamlConverter.Model.Paint
{
    public record GradientStop : Resource
    {
        public float Offset { get; init; }

        public ShimSkiaSharp.SKColor Color { get; init; }
    }
}
