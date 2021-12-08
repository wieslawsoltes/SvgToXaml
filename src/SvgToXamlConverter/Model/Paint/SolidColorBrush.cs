namespace SvgToXamlConverter.Model.Paint;

public record SolidColorBrush : Brush
{
    public ShimSkiaSharp.SKColor Color { get; init; }
}