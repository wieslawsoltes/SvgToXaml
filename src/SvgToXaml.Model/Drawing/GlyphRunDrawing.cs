using SvgToXaml.Model.Paint;

namespace SvgToXaml.Model.Drawing;

public record GlyphRunDrawing : Drawing
{
    public Brush? Foreground { get; init; }
    
    public GlyphRun? GlyphRun { get; init; }
}
