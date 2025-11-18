using SvgToXaml.Model.Resources;

namespace SvgToXaml.Model.Drawing;

public record ImageDrawing : Drawing
{
    public SkiaSharp.SKRect Rect { get; init; }
    
    public string? ImageSource { get; init; }
}
