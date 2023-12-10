using System.Collections.Generic;
using SvgToXaml.Model.Paint;

namespace SvgToXaml.Model.Resources;

public record ResourceDictionary
{
    public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, Brush Brush)> Brushes  { get; init; } = new();

    public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, Pen Pen)> Pens  { get; init; } = new();

    public int BrushCounter { get; set; }

    public int PenCounter { get; set; }

    public HashSet<Brush> UseBrushes { get; init; } = new();

    public HashSet<Pen> UsePens { get; init; } = new();
}
