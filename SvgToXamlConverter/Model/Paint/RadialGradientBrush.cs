namespace SvgToXamlConverter
{
    public record RadialGradientBrush : GradientBrush
    {
        public SkiaSharp.SKPoint Center { get; init; }

        public float Radius { get; init; }
    }
}
