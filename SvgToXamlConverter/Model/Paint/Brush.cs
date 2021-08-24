namespace SvgToXamlConverter
{
    public abstract record Brush : Resource
    {
        public SkiaSharp.SKRect Bounds { get; init; }

        public SkiaSharp.SKMatrix? LocalMatrix { get; init; }
  
        public SkiaSharp.SKMatrix WithTransXY(SkiaSharp.SKMatrix matrix, float x, float y)
        {
            return new SkiaSharp.SKMatrix(
                matrix.ScaleX,
                matrix.SkewX,
                matrix.TransX - x,
                matrix.SkewY,
                matrix.ScaleY,
                matrix.TransY - y,
                matrix.Persp0,
                matrix.Persp1,
                matrix.Persp2);
        }
    }
}
