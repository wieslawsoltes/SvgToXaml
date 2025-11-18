using SkiaSharp;
using System;

namespace SvgToXaml.Model.Drawing;

public record GlyphRun
{
    public SKPoint BaselineOrigin { get; init; }
    public Uri? GlyphTypeface { get; init; }
    public double FontRenderingEmSize { get; init; }
    public ushort[]? GlyphIndices { get; init; }
    public double[]? AdvanceWidths { get; init; }
    public SKPoint[]? GlyphOffsets { get; init; }
    public char[]? Characters { get; init; }
    public int BidiLevel { get; init; }
    public string? Language { get; init; }
    public string? DeviceFontName { get; init; }
    public bool IsSideways { get; init; }
    public ushort[]? ClusterMap { get; init; }
    public bool[]? CaretStops { get; init; }
}
