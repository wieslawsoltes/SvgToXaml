namespace SvgToXamlConverter
{
    public record SolidColorBrush : Brush
    {
        public ShimSkiaSharp.SKColor Color { get; init; }
    }
}
