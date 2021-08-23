using System.Linq;
using System.Text;

namespace SvgToXamlConverter
{
    public record GeometryDrawing : Drawing
    {
        public ShimSkiaSharp.SKPaint? Paint { get; }

        public SkiaSharp.SKPath? Geometry { get; }

        public Brush? Brush { get; }

        public Pen? Pen { get; }

        public GeometryDrawing(ShimSkiaSharp.SKPaint? paint = null, SkiaSharp.SKPath? geometry = null, ResourceDictionary? resources = null)
        {
            Paint = paint;
            Geometry = geometry;

            if (Paint is { } && Geometry is { })
            {
                var isFilled = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Fill;

                if (isFilled && Paint.Shader is { })
                {
                    var resourceKey = resources is { } ? $"Brush{resources.BrushCounter++}" : null;
                    
                    Brush = Factory.CreateBrush(Paint.Shader, Geometry.Bounds, resourceKey);

                    if (resources is { } && Brush?.Key is { })
                    {
                        resources.Brushes.Add(Brush.Key, (Paint, Brush));
                    }
                }

                var isStroked = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Stroke;

                if (isStroked && Paint.Shader is { })
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

        public override string Generate(GeneratorContext context)
        {
            if (Paint is null || Geometry is null)
            {
                return "";
            }
 
            var sb = new StringBuilder();
            
            sb.Append($"<GeometryDrawing");

            var isFilled = Brush is { };
            var isStroked = Pen is { };

            if (isFilled && Brush is SolidColorBrush solidColorBrush && context.Resources is null)
            {
                sb.Append($" Brush=\"{XamlConverter.ToHexColor(solidColorBrush.Color)}\"");
            }

            var brush = default(Brush);
            var pen = default(Pen);

            if (isFilled && Brush is { } and not SolidColorBrush && context.Resources is null)
            {
                brush = Brush;
            }

            if (isFilled && Paint is { } && context.Resources is { })
            {
                bool haveBrush = false;

                if (context.ReuseExistingResources)
                {
                    var existingBrush = context.Resources.Brushes.FirstOrDefault(x =>
                    {
                        if (x.Value.Paint.Shader is { } && x.Value.Paint.Shader.Equals(Paint.Shader))
                        {
                            return true;
                        }

                        return false;
                    });

                    if (!string.IsNullOrEmpty(existingBrush.Key))
                    {
                        sb.Append($" Brush=\"{{DynamicResource {existingBrush.Key}}}\"");
                        haveBrush = true;
                    }
                }

                if (!haveBrush)
                {
                    if (Brush is { } && context.Resources is { } && Brush.Key is { })
                    {
                        sb.Append($" Brush=\"{{DynamicResource {Brush.Key}}}\"");
                        haveBrush = true;
                    }
                    
                    if (!haveBrush)
                    {
                        brush = Brush;
                    }
                }
            }

            if (isStroked && Pen is { } && context.Resources is null)
            {
                pen = Pen;
            }

            if (isStroked && Paint is { } && context.Resources is { })
            {
                bool havePen = false;

                if (context.ReuseExistingResources)
                {
                    var existingPen = context.Resources.Pens.FirstOrDefault(x =>
                    {
                        if (x.Value.Paint.Shader is { } 
                            && x.Value.Paint.Shader.Equals(Paint.Shader)
                            && x.Value.Paint.StrokeWidth.Equals(Paint.StrokeWidth)
                            && x.Value.Paint.StrokeCap.Equals(Paint.StrokeCap)
                            && x.Value.Paint.PathEffect == Paint.PathEffect
                            && x.Value.Paint.StrokeJoin.Equals(Paint.StrokeJoin)
                            && x.Value.Paint.StrokeMiter.Equals(Paint.StrokeMiter))
                        {
                            return true;
                        }

                        return false;
                    });

                    if (!string.IsNullOrEmpty(existingPen.Key))
                    {
                        sb.Append($" Pen=\"{{DynamicResource {existingPen.Key}}}\"");
                        havePen = true;
                    }
                }

                if (!havePen)
                {
                    if (Pen is { } && context.Resources is { } && Pen.Key is { })
                    {
                        sb.Append($" Pen=\"{{DynamicResource {Pen.Key}}}\"");
                        havePen = true;
                    }

                    if (!havePen)
                    {
                        pen = Pen;
                    }
                }
            }

            if (Geometry is { })
            {
                sb.Append($" Geometry=\"{XamlConverter.ToSvgPathData(Geometry)}\"");
            }

            if (brush is { } || pen is { })
            {
                sb.Append($">{context.NewLine}");
            }
            else
            {
                sb.Append($"/>{context.NewLine}");
            }

            if (brush is { })
            {
                sb.Append($"  <GeometryDrawing.Brush>{context.NewLine}");
                sb.Append(brush.Generate(context));
                sb.Append($"  </GeometryDrawing.Brush>{context.NewLine}");
            }

            if (pen is { })
            {
                sb.Append($"  <GeometryDrawing.Pen>{context.NewLine}");
                sb.Append(pen.Generate(context));
                sb.Append($"  </GeometryDrawing.Pen>{context.NewLine}");
            }

            if (brush is { } || pen is { })
            {
                sb.Append($"</GeometryDrawing>{context.NewLine}");
            }

            return sb.ToString();
        }
    }
}
