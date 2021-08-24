namespace SvgToXamlConverter
{
    public record Pen : Resource
    {
        public SkiaSharp.SKRect Bounds { get; init; }

        public Brush? Brush { get; init; }

        public float StrokeWidth { get; init; }

        public ShimSkiaSharp.SKStrokeCap StrokeCap { get; init; }

        public ShimSkiaSharp.SKStrokeJoin StrokeJoin { get; init; }

        public float StrokeMiter { get; init; }

        public Dashes? Dashes { get; init; }
    }
}
