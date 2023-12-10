namespace SvgToXaml.Model.Paint;

public record LinearGradientBrush : GradientBrush
{
    public SkiaSharp.SKPoint Start { get; init; }

    public SkiaSharp.SKPoint End { get; init; }
}
