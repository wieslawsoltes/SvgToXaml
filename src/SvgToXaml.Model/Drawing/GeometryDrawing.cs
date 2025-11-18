using SvgToXaml.Model.Paint;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Model.Drawing;

public record GeometryDrawing : Drawing
{
    public ShimSkiaSharp.SKPaint? Paint { get; }

    public SkiaSharp.SKPath? Geometry { get; }

    public Brush? Brush { get; }

    public Pen? Pen { get; }

    public GeometryDrawing(
        ShimSkiaSharp.SKPaint? paint = null, 
        SkiaSharp.SKPath? geometry = null, 
        ResourceDictionary? resources = null)
    {
        Paint = paint;
        Geometry = geometry;

        if (Paint is { } && Geometry is { })
        {
            var isFilled = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Fill;

            if (isFilled)
            {
                var resourceKey = resources is { } ? $"Brush{resources.BrushCounter++}" : null;

                if (Paint.Shader is { })
                {
                    Brush = Factory.CreateBrush(Paint.Shader, Geometry.Bounds, resourceKey);
                }
                else
                {
                    Brush = new SolidColorBrush
                    {
                        Key = resourceKey,
                        Color = Paint.Color ?? new ShimSkiaSharp.SKColor(0, 0, 0, 255)
                    };
                }

                if (resources is { } && Brush?.Key is { })
                {
                    resources.Brushes.Add(Brush.Key, (Paint, Brush));
                }
            }

            var isStroked = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Stroke;

            if (isStroked)
            {
                var resourceKey = resources is { } ? $"Pen{resources.PenCounter++}" : null;

                Pen = Factory.CreatePen(Paint, Geometry.Bounds, resourceKey);

                if (resources is { } && Pen?.Key is { })
                {
                    resources.Pens.Add(Pen.Key, (Paint, Pen));
                }
            }
        }
    }
}
