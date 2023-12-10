using System.Collections.Generic;

namespace SvgToXaml.Model.Paint;

public abstract record GradientBrush : Brush
{
    public ShimSkiaSharp.SKShaderTileMode Mode { get; init; }

    public List<GradientStop> GradientStops { get; init; } = new ();
}
