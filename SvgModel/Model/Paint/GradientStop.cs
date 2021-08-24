namespace SvgToXamlConverter
{
    public record GradientStop : Resource
    {
        public float Offset { get; init; }

        public ShimSkiaSharp.SKColor Color { get; init; }
    }
}
