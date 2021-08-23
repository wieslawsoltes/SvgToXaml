using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvgToXamlConverter
{
    public record GeneratorContext
    {
        public string NewLine { get; init; } = "\r\n";

        public bool UseCompatMode { get; init; } = false;

        public bool UseBrushTransform { get; init; } = false;

        public bool ReuseExistingResources { get; init; } = false;

        public  bool WriteResources { get; init; } = false;

        public ResourceDictionary? Resources { get; init; }
    }

    public interface IGenerator
    {
        string Generate(GeneratorContext context);
    }

    public abstract record Resource : IGenerator
    {
        public string? Key { get; init; }

        public abstract string Generate(GeneratorContext context);
    }

    public record ResourceDictionary : IGenerator
    {
        public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, Brush Brush)> Brushes  { get; init; } = new();

        public Dictionary<string, (ShimSkiaSharp.SKPaint Paint, Pen Pen)> Pens  { get; init; } = new();

        public int BrushCounter { get; set; }

        public int PenCounter { get; set; }

        public string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            foreach (var resource in Brushes)
            {
                sb.Append(resource.Value.Brush.Generate(context));
            }

            foreach (var resource in Pens)
            {
                sb.Append(resource.Value.Pen.Generate(context));
            }

            return sb.ToString();
        }
    }

    public abstract record Brush : Resource
    {
        public SkiaSharp.SKRect Bounds { get; init; }

        public SkiaSharp.SKMatrix? LocalMatrix { get; init; }
  
        protected static SkiaSharp.SKMatrix WithTransXY(SkiaSharp.SKMatrix matrix, float x, float y)
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

    public record SolidColorBrush : Brush
    {
        public ShimSkiaSharp.SKColor Color { get; init; } 

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            sb.Append($"<SolidColorBrush{XamlConverter.ToKey(Key)}");
            sb.Append($" Color=\"{XamlConverter.ToHexColor(Color)}\"");
            sb.Append($"/>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record GradientStop : Resource
    {
        public float Offset { get; init; }

        public ShimSkiaSharp.SKColor Color { get; init; } 

        public override string Generate(GeneratorContext context)
        {
            return $"<GradientStop Offset=\"{XamlConverter.ToString(Offset)}\" Color=\"{XamlConverter.ToHexColor(Color)}\"/>{context.NewLine}";
        }
    }

    public abstract record GradientBrush : Brush
    {
        public ShimSkiaSharp.SKShaderTileMode Mode { get; init; }

        public List<GradientStop> GradientStops { get; init; } = new ();
    }

    public record LinearGradientBrush : GradientBrush
    {
        public SkiaSharp.SKPoint Start { get; init; }

        public SkiaSharp.SKPoint End { get; init; }

        public override string Generate(GeneratorContext context)
        {
          var sb = new StringBuilder();

            var start = Start;
            var end = End;

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix =LocalMatrix.Value;
                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - Bounds.Location.Y);

                    start = localMatrix.MapPoint(start);
                    end = localMatrix.MapPoint(end);
                }
                else
                {
                    start.X = Math.Max(0f, start.X - Bounds.Location.X);
                    start.Y = Math.Max(0f, start.Y - Bounds.Location.Y);
                    end.X = Math.Max(0f, end.X - Bounds.Location.X);
                    end.Y = Math.Max(0f, end.Y - Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (LocalMatrix is null)
                    {
                        start.X = Math.Max(0f, start.X - Bounds.Location.X);
                        start.Y = Math.Max(0f, start.Y - Bounds.Location.Y);
                        end.X = Math.Max(0f, end.X - Bounds.Location.X);
                        end.Y = Math.Max(0f, end.Y - Bounds.Location.Y);
                    }
                }
            }

            sb.Append($"<LinearGradientBrush{XamlConverter.ToKey(Key)}");

            sb.Append($" StartPoint=\"{XamlConverter.ToPoint(start)}\"");
            sb.Append($" EndPoint=\"{XamlConverter.ToPoint(end)}\"");

            if (Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{XamlConverter.ToGradientSpreadMethod(Mode)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on LinearGradientBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <LinearGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </LinearGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (GradientStops.Count > 0)
            {
                sb.Append($"  <LinearGradientBrush.GradientStops>{context.NewLine}");

                for (var i = 0; i < GradientStops.Count; i++)
                {
                    var color = XamlConverter.ToHexColor(GradientStops[i].Color);
                    var offset = XamlConverter.ToString(GradientStops[i].Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </LinearGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</LinearGradientBrush>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record RadialGradientBrush : GradientBrush
    {
        public SkiaSharp.SKPoint Center { get; init; }

        public float Radius { get; init; }

        public override string Generate(GeneratorContext context)
        {
          var sb = new StringBuilder();

            var radius = Radius;

            var center = Center;
            var gradientOrigin = Center;

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix = LocalMatrix.Value;

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - Bounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radiusMapped = localMatrix.MapVector(new SkiaSharp.SKPoint(radius, 0));
                    radius = radiusMapped.X;
                }
                else
                {
                    center.X = Math.Max(0f, center.X - Bounds.Location.X);
                    center.Y = Math.Max(0f, center.Y - Bounds.Location.Y);
                    gradientOrigin.X = Math.Max(0f, gradientOrigin.X - Bounds.Location.X);
                    gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (LocalMatrix is null)
                    {
                        center.X = Math.Max(0f, center.X - Bounds.Location.X);
                        center.Y = Math.Max(0f, center.Y - Bounds.Location.Y);
                        gradientOrigin.X = Math.Max(0f, gradientOrigin.X - Bounds.Location.X);
                        gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - Bounds.Location.Y);
                    }
                }
            }

            if (!context.UseCompatMode)
            {
                radius = radius / Bounds.Width;
            }

            sb.Append($"<RadialGradientBrush{XamlConverter.ToKey(Key)}");

            sb.Append($" Center=\"{XamlConverter.ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{XamlConverter.ToPoint(gradientOrigin)}\"");

            if (context.UseCompatMode)
            {
                sb.Append($" RadiusX=\"{XamlConverter.ToString(radius)}\"");
                sb.Append($" RadiusY=\"{XamlConverter.ToString(radius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{XamlConverter.ToString(radius)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            if (Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{XamlConverter.ToGradientSpreadMethod(Mode)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on RadialGradientBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <RadialGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </RadialGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (GradientStops.Count > 0)
            {
                sb.Append($"  <RadialGradientBrush.GradientStops>{context.NewLine}");

                for (var i = 0; i < GradientStops.Count; i++)
                {
                    var color = XamlConverter.ToHexColor(GradientStops[i].Color);
                    var offset = XamlConverter.ToString(GradientStops[i].Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </RadialGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</RadialGradientBrush>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record TwoPointConicalGradientBrush : GradientBrush
    {
        public float StartRadius { get; init; }

        public float EndRadius { get; init; }

        public SkiaSharp.SKPoint Start { get; init; }

        public SkiaSharp.SKPoint End { get; init; }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
            // ReSharper disable once UnusedVariable
            var startRadius = StartRadius;

            // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
            // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
            var endRadius = EndRadius;

            var center = End;
            var gradientOrigin = Start;

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix = LocalMatrix.Value;

                    localMatrix.TransX = Math.Max(0f, localMatrix.TransX - Bounds.Location.X);
                    localMatrix.TransY = Math.Max(0f, localMatrix.TransY - Bounds.Location.Y);

                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radiusMapped = localMatrix.MapVector(new SkiaSharp.SKPoint(endRadius, 0));
                    endRadius = radiusMapped.X;
                }
                else
                {
                    center.X = Math.Max(0f, center.X - Bounds.Location.X);
                    center.Y = Math.Max(0f, center.Y - Bounds.Location.Y);
                    gradientOrigin.X = Math.Max(0f, gradientOrigin.X - Bounds.Location.X);
                    gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - Bounds.Location.Y);
                }
            }
            else
            {
                if (!context.UseCompatMode)
                {
                    if (LocalMatrix is null)
                    {
                        center.X = Math.Max(0f, center.X - Bounds.Location.X);
                        center.Y = Math.Max(0f, center.Y - Bounds.Location.Y);
                        gradientOrigin.X = Math.Max(0f, gradientOrigin.X - Bounds.Location.X);
                        gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - Bounds.Location.Y);
                    }
                }
            }

            if (!context.UseCompatMode)
            {
                endRadius = endRadius / Bounds.Width;
            }

            sb.Append($"<RadialGradientBrush{XamlConverter.ToKey(Key)}");

            sb.Append($" Center=\"{XamlConverter.ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{XamlConverter.ToPoint(gradientOrigin)}\"");

            if (context.UseCompatMode)
            {
                sb.Append($" RadiusX=\"{XamlConverter.ToString(endRadius)}\"");
                sb.Append($" RadiusY=\"{XamlConverter.ToString(endRadius)}\"");
            }
            else
            {
                sb.Append($" Radius=\"{XamlConverter.ToString(endRadius)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" MappingMode=\"Absolute\"");
            }

            if (Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{XamlConverter.ToGradientSpreadMethod(Mode)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on RadialGradientBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <RadialGradientBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </RadialGradientBrush.Transform>{context.NewLine}");
                }
            }

            if (GradientStops.Count > 0)
            {
                sb.Append($"  <RadialGradientBrush.GradientStops>{context.NewLine}");

                for (var i = 0; i < GradientStops.Count; i++)
                {
                    var color = XamlConverter.ToHexColor(GradientStops[i].Color);
                    var offset = XamlConverter.ToString(GradientStops[i].Offset);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{context.NewLine}");
                }

                sb.Append($"  </RadialGradientBrush.GradientStops>{context.NewLine}");
            }

            sb.Append($"</RadialGradientBrush>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record PictureBrush : GradientBrush
    {
        public Image? Picture { get; init; }

        public ShimSkiaSharp.SKRect CullRect { get; init; }

        public ShimSkiaSharp.SKRect Tile { get; init; }

        public ShimSkiaSharp.SKShaderTileMode TileMode { get; init; }
 
        public override string Generate(GeneratorContext context)
        {
            if (Picture is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            if (!context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    var localMatrix = LocalMatrix.Value;

                    if (!localMatrix.IsIdentity)
                    {
#if DEBUG
                        sb.Append($"<!-- TODO: Transform: {XamlConverter.ToMatrix(localMatrix)} -->{context.NewLine}");
#endif
                    }
                }
                else
                {
                    // TODO: Adjust using Bounds.Location ?
                }
            }

            var sourceRect = CullRect;
            var destinationRect = Tile;

            // TODO: Use different visual then Image ?
            sb.Append($"<VisualBrush{XamlConverter.ToKey(Key)}");

            if (TileMode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" TileMode=\"{XamlConverter.ToTileMode(TileMode)}\"");
            }

            if (context.UseCompatMode)
            {
                sb.Append($" Viewport=\"{XamlConverter.ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                sb.Append($" Viewbox=\"{XamlConverter.ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
            }
            else
            {
                sb.Append($" SourceRect=\"{XamlConverter.ToRect(sourceRect)}\"");
                sb.Append($" DestinationRect=\"{XamlConverter.ToRect(destinationRect)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.UseBrushTransform)
            {
                if (LocalMatrix is { })
                {
                    // TODO: Missing Transform property on VisualBrush
                    var localMatrix = LocalMatrix.Value;

                    if (!context.UseCompatMode)
                    {
                        localMatrix = WithTransXY(localMatrix, Bounds.Location.X, Bounds.Location.Y);
                    }

                    sb.Append($"  <VisualBrush.Transform>{context.NewLine}");
                    sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(localMatrix)}\"/>{context.NewLine}");
                    sb.Append($"  </VisualBrush.Transform>{context.NewLine}");
                }
            }

            if (Picture is not null)
            {
                sb.Append($"  <VisualBrush.Visual>{context.NewLine}");
                sb.Append(Picture.Generate(context));
                sb.Append($"{context.NewLine}");
                sb.Append($"  </VisualBrush.Visual>{context.NewLine}");
            }

            sb.Append($"</VisualBrush>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record Dashes
    {
        public float[]? Intervals { get; init; }

        public float Phase { get; init; }
    }
    
    public record Pen : Resource
    {
        public SkiaSharp.SKRect Bounds { get; init; }

        public Brush? Brush { get; init; }

        public float StrokeWidth { get; init; }

        public ShimSkiaSharp.SKStrokeCap StrokeCap { get; init; }

        public ShimSkiaSharp.SKStrokeJoin StrokeJoin { get; init; }

        public float StrokeMiter { get; init; }

        public Dashes? Dashes { get; init; }

        public override string Generate(GeneratorContext context)
        {
            if (Brush is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<Pen{XamlConverter.ToKey(Key)}");

            if (Brush is SolidColorBrush solidColorBrush)
            {
                sb.Append($" Brush=\"{XamlConverter.ToHexColor(solidColorBrush.Color)}\"");
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (StrokeWidth != 1.0)
            {
                sb.Append($" Thickness=\"{XamlConverter.ToString(StrokeWidth)}\"");
            }

            if (StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
            {
                if (context.UseCompatMode)
                {
                    sb.Append($" StartLineCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                    sb.Append($" EndLineCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                }
                else
                {
                    sb.Append($" LineCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                }
            }

            if (context.UseCompatMode)
            {
                if (Dashes is { Intervals: { } })
                {
                    if (StrokeCap != ShimSkiaSharp.SKStrokeCap.Square)
                    {
                        sb.Append($" DashCap=\"{XamlConverter.ToPenLineCap(StrokeCap)}\"");
                    }
                }

                if (StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Miter)
                {
                    sb.Append($" LineJoin=\"{XamlConverter.ToPenLineJoin(StrokeJoin)}\"");
                }
            }
            else
            {
                if (StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
                {
                    sb.Append($" LineJoin=\"{XamlConverter.ToPenLineJoin(StrokeJoin)}\"");
                }
            }

            if (context.UseCompatMode)
            {
                var miterLimit = StrokeMiter;
                var strokeWidth = StrokeWidth;

                if (miterLimit < 1.0f)
                {
                    miterLimit = 10.0f;
                }
                else
                {
                    if (strokeWidth <= 0.0f)
                    {
                        miterLimit = 1.0f;
                    }
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (miterLimit != 10.0)
                {
                    sb.Append($" MiterLimit=\"{XamlConverter.ToString(miterLimit)}\"");
                }
            }
            else
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (StrokeMiter != 10.0)
                {
                    sb.Append($" MiterLimit=\"{XamlConverter.ToString(StrokeMiter)}\"");
                }
            }

            if (Brush is not SolidColorBrush || Dashes is { Intervals: { } })
            {
                sb.Append($">{context.NewLine}");
            }
            else
            {
                sb.Append($"/>{context.NewLine}");
            }

            if (Dashes is { Intervals: { } })
            {
                var dashes = new List<double>();

                foreach (var interval in Dashes.Intervals)
                {
                    dashes.Add(interval / StrokeWidth);
                }

                var offset = Dashes.Phase / StrokeWidth;

                sb.Append($"  <Pen.DashStyle>{context.NewLine}");
                sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(XamlConverter.ToString))}\" Offset=\"{XamlConverter.ToString(offset)}\"/>{context.NewLine}");
                sb.Append($"  </Pen.DashStyle>{context.NewLine}");
            }

            if (Brush is not SolidColorBrush)
            {
                sb.Append($"  <Pen.Brush>{context.NewLine}");
                sb.Append(Brush.Generate(context));
                sb.Append($"  </Pen.Brush>{context.NewLine}");
            }

            if (Brush is not SolidColorBrush || Dashes is { Intervals: { } })
            {
                sb.Append($"</Pen>{context.NewLine}");
            }

            return sb.ToString();
        }
    }

    public abstract record Drawing : Resource;

    public record GeometryDrawing : Drawing
    {
        public ShimSkiaSharp.SKPaint? Paint { get; }

        public SkiaSharp.SKPath? Geometry { get; }

        public Brush? Brush { get; }

        public Pen? Pen { get; }

        public GeometryDrawing(ShimSkiaSharp.SKPaint? paint, SkiaSharp.SKPath? geometry, ResourceDictionary? resources)
        {
            Paint = paint;
            Geometry = geometry;

            if (Paint is { } && Geometry is { })
            {
                var isFilled = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Fill;
                var isStroked = Paint.Style is ShimSkiaSharp.SKPaintStyle.StrokeAndFill or ShimSkiaSharp.SKPaintStyle.Stroke;

                if (isFilled && Paint.Shader is { })
                {
                    var resourceKey = resources is { } ? $"Brush{resources.BrushCounter++}" : null;

                    Brush = ToBrushModel(Paint.Shader, Geometry.Bounds, resourceKey);

                    if (Brush is { } && resources is { } && resourceKey is { })
                    {
                        resources.Brushes.Add(resourceKey, (Paint, Brush));
                    }
                }

                if (isStroked && Pen is { })
                {
                    var resourceKey = resources is { } ? $"Pen{resources.PenCounter++}" : null;

                    Pen = ToPenModel(Paint, Geometry.Bounds, resourceKey);

                    if (Pen is { } && resources is { } && resourceKey is { })
                    {
                        resources.Pens.Add(resourceKey, (Paint, Pen));
                    }
                }
            }
        }

        private Brush ToBrushModel(ShimSkiaSharp.ColorShader colorShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var brush = new SolidColorBrush
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = null,
                Color = colorShader.Color
            };

            return brush;
        }

        private LinearGradientBrush ToBrushModel(ShimSkiaSharp.LinearGradientShader linearGradientShader, SkiaSharp.SKRect skBounds, string? key)
        {
            var brush = new LinearGradientBrush
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = linearGradientShader.LocalMatrix is null
                    ? null
                    : Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value),
                Start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start),
                End = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End),
                Mode = linearGradientShader.Mode
            };

            if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
            {
                for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                {
                    var color = linearGradientShader.Colors[i];
                    var offset = linearGradientShader.ColorPos[i];
                    brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
                }
            }

            return brush;
        }

        private Brush ToBrushModel(ShimSkiaSharp.RadialGradientShader radialGradientShader, SkiaSharp.SKRect skBounds, string? key)
        {
            var brush = new RadialGradientBrush
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = radialGradientShader.LocalMatrix is null
                    ? null
                    : Svg.Skia.SkiaModelExtensions.ToSKMatrix(radialGradientShader.LocalMatrix.Value),
                Center = Svg.Skia.SkiaModelExtensions.ToSKPoint(radialGradientShader.Center),
                Radius = radialGradientShader.Radius,
                Mode = radialGradientShader.Mode
            };

            if (radialGradientShader.Colors is { } && radialGradientShader.ColorPos is { })
            {
                for (var i = 0; i < radialGradientShader.Colors.Length; i++)
                {
                    var color = radialGradientShader.Colors[i];
                    var offset = radialGradientShader.ColorPos[i];
                    brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
                }
            }

            return brush;
        }

        private Brush ToBrushModel(ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader, SkiaSharp.SKRect skBounds, string? key)
        {
            var brush = new TwoPointConicalGradientBrush()
            {
                Key = key,
                Bounds = skBounds,
                LocalMatrix = twoPointConicalGradientShader.LocalMatrix is null
                    ? null
                    : Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value),
                Start = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.Start),
                End = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.End),
                StartRadius = twoPointConicalGradientShader.StartRadius,
                EndRadius = twoPointConicalGradientShader.EndRadius,
                Mode = twoPointConicalGradientShader.Mode
            };

            if (twoPointConicalGradientShader.Colors is { } && twoPointConicalGradientShader.ColorPos is { })
            {
                for (var i = 0; i < twoPointConicalGradientShader.Colors.Length; i++)
                {
                    var color = twoPointConicalGradientShader.Colors[i];
                    var offset = twoPointConicalGradientShader.ColorPos[i];
                    brush.GradientStops.Add(new GradientStop { Color = color, Offset = offset });
                }
            }

            return brush;
        }

        private Brush? ToBrushModel(ShimSkiaSharp.PictureShader pictureShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            var brush = new PictureBrush
            {
                Key = key,
                Bounds = skBounds,
                Picture = new Image(new DrawingImage(new DrawingGroup(pictureShader.Src))),
                CullRect =  pictureShader.Src?.CullRect ?? ShimSkiaSharp.SKRect.Empty,
                Tile = pictureShader.Tile,
                TileMode = pictureShader.TmX
            };

            return brush;
        }

        private Brush? ToBrushModel(ShimSkiaSharp.SKShader skShader, SkiaSharp.SKRect skBounds, string? key = null)
        {
            return skShader switch
            {
                ShimSkiaSharp.ColorShader colorShader => ToBrushModel(colorShader, skBounds, key),
                ShimSkiaSharp.LinearGradientShader linearGradientShader => ToBrushModel(linearGradientShader, skBounds, key),
                ShimSkiaSharp.RadialGradientShader radialGradientShader => ToBrushModel(radialGradientShader, skBounds, key),
                ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader => ToBrushModel(twoPointConicalGradientShader, skBounds, key),
                ShimSkiaSharp.PictureShader pictureShader => ToBrushModel(pictureShader, skBounds, key),
                _ => null
            };
        }

        private Pen? ToPenModel(ShimSkiaSharp.SKPaint skPaint, SkiaSharp.SKRect skBounds, string? key)
        {
            if (skPaint.Shader is null)
            {
                return null;
            }

            var pen = new Pen
            {
                Key = key,
                Bounds = skBounds,
                Brush = ToBrushModel(skPaint.Shader, skBounds),
                StrokeWidth = skPaint.StrokeWidth,
                StrokeCap = skPaint.StrokeCap,
                StrokeJoin = skPaint.StrokeJoin,
                StrokeMiter = skPaint.StrokeMiter,
                Dashes = skPaint.PathEffect is ShimSkiaSharp.DashPathEffect(var intervals, var phase) { Intervals: { } }
                    ? new Dashes { Intervals = intervals, Phase = phase }
                    : null
            };

            return pen;
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
                    brush = Brush;
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
                    pen = Pen;
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
                sb.Append($"{brush}");
                sb.Append($"  </GeometryDrawing.Brush>{context.NewLine}");
            }

            if (pen is { })
            {
                sb.Append($"  <GeometryDrawing.Pen>{context.NewLine}");
                sb.Append($"{pen}");
                sb.Append($"  </GeometryDrawing.Pen>{context.NewLine}");
            }

            if (brush is { } || pen is { })
            {
                sb.Append($"</GeometryDrawing>{context.NewLine}");
            }

            return sb.ToString();
        }
    }

    public record DrawingGroup : Drawing
    {
        private const byte OpaqueAlpha = 255;

        private static readonly ShimSkiaSharp.SKColor s_transparentBlack = new(0, 0, 0, 255);

        private enum LayerType
        {
            UnknownPaint,
            MaskGroup,
            MaskBrush,
            OpacityGroup,
            FilterGroup
        }

        public ShimSkiaSharp.SKPicture? Picture { get; }

        public double Opacity { get; }

        public SkiaSharp.SKMatrix? Transform { get; }

        public SkiaSharp.SKPath? ClipGeometry { get; }

        public Brush? OpacityMask { get; }

        public List<Drawing> Children { get; } = new();

        public DrawingGroup(ShimSkiaSharp.SKPicture? picture)
        {
            Picture = picture;

/*
            if (skPicture?.Commands is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup{XamlConverter.ToKey(key)}>{NewLine}");

            var totalMatrixStack = new Stack<SkiaSharp.SKMatrix?>();
            var currentTotalMatrix = default(SkiaSharp.SKMatrix?);

            var clipPathStack = new Stack<SkiaSharp.SKPath?>();
            var currentClipPath = default(SkiaSharp.SKPath?);

            var layersStack = new Stack<(StringBuilder Builder, LayerType Type, object? Value)?>();

            foreach (var canvasCommand in skPicture.Commands)
            {
                switch (canvasCommand)
                {
                    case ShimSkiaSharp.ClipPathCanvasCommand(var clipPath, _, _):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                        if (path is null)
                        {
                            break;
                        }

                        var clipGeometry = XamlConverter.ToSvgPathData(path);

                        Debug($"StartClipPath({clipPathStack.Count})");

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");

                        currentClipPath = path;

                        break;
                    }
                    case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                    {
                        var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                        var path = new SkiaSharp.SKPath();
                        path.AddRect(rect);

                        var clipGeometry = XamlConverter.ToSvgPathData(path);

                        Debug($"StarClipPath({clipPathStack.Count})");

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        sb.Append($"  </DrawingGroup.ClipGeometry>{NewLine}");

                        currentClipPath = path;

                        break;
                    }
                    case ShimSkiaSharp.SetMatrixCanvasCommand(var skMatrix):
                    {
                        var matrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(skMatrix);
                        if (matrix.IsIdentity)
                        {
                            break;
                        }

                        var previousMatrixList = new List<SkiaSharp.SKMatrix>();

                        foreach (var totalMatrixList in totalMatrixStack)
                        {
                            if (totalMatrixList is { } totalMatrix)
                            {
                                previousMatrixList.Add(totalMatrix);
                            }
                        }

                        previousMatrixList.Reverse();

                        foreach (var previousMatrix in previousMatrixList)
                        {
                            var inverted = previousMatrix.Invert();
                            matrix = inverted.PreConcat(matrix);
                        }

                        Debug($"StarMatrix({totalMatrixStack.Count})");

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append($"  <DrawingGroup.Transform>{NewLine}");
                        sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(matrix)}\"/>{NewLine}");
                        sb.Append($"  </DrawingGroup.Transform>{NewLine}");

                        currentTotalMatrix = matrix;

                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(var count, var skPaint):
                    {
                        // Mask

                        if (skPaint is null)
                        {
                            break;
                        }

                        var isMaskBrush = skPaint.Shader is null 
                                          && skPaint.ColorFilter is { }
                                          && skPaint.ImageFilter is null 
                                          && skPaint.Color is { } skMaskEndColor 
                                          && skMaskEndColor.Equals(s_transparentBlack);
                        if (isMaskBrush)
                        {
                            SaveLayer(LayerType.MaskBrush, skPaint, count);

                            break;
                        }

                        var isMaskGroup = skPaint.Shader is null 
                                          && skPaint.ColorFilter is null 
                                          && skPaint.ImageFilter is null 
                                          && skPaint.Color is { } skMaskStartColor 
                                          && skMaskStartColor.Equals(s_transparentBlack);
                        if (isMaskGroup)
                        {
                            SaveLayer(LayerType.MaskGroup, skPaint, count);

                            break;
                        }

                        // Opacity

                        var isOpacityGroup = skPaint.Shader is null 
                                             && skPaint.ColorFilter is null 
                                             && skPaint.ImageFilter is null 
                                             && skPaint.Color is { Alpha: < OpaqueAlpha };
                        if (isOpacityGroup)
                        {
                            SaveLayer(LayerType.OpacityGroup, skPaint, count);

                            break;
                        }

                        // Filter

                        var isFilterGroup = skPaint.Shader is null 
                                            && skPaint.ColorFilter is null
                                            && skPaint.ImageFilter is { } 
                                            && skPaint.Color is { } skFilterColor
                                            && skFilterColor.Equals(s_transparentBlack);
                        if (isFilterGroup)
                        {
                            SaveLayer(LayerType.FilterGroup, skPaint, count);

                            break;
                        }

                        SaveLayer(LayerType.UnknownPaint, skPaint, count);

                        break;
                    }
                    case ShimSkiaSharp.SaveCanvasCommand(var count):
                    {
                        EmptyLayer();
                        Save(count);

                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand(var count):
                    {
                        Restore(count);

                        break;
                    }
                    case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        ToXamlGeometryDrawing(path, skPaint, sb, resources, reuseExistingResources);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextCanvasCommand(var text, var x, var y, var skPaint):
                    {
                        var paint = Svg.Skia.SkiaModelExtensions.ToSKPaint(skPaint);
                        var path = paint.GetTextPath(text, x, y);
                        if (path.IsEmpty)
                        {
                            break;
                        }

                        Debug($"Text='{text}'");

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Center)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width / 2f, 0f));
                        }

                        if (skPaint.TextAlign == ShimSkiaSharp.SKTextAlign.Right)
                        {
                            path.Transform(SkiaSharp.SKMatrix.CreateTranslation(-path.Bounds.Width, 0f));
                        }

                        ToXamlGeometryDrawing(path, skPaint, sb, resources, reuseExistingResources);

                        break;
                    }
                    case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: TextOnPath");

                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: TextBlob");

                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: Image");

                        break;
                    }
                }
            }

            void Debug(string message)
            {
#if DEBUG
                sb.Append($"<!-- {message} -->{NewLine}");
#endif
            }

            void EmptyLayer()
            {
                layersStack.Push(null);
            }

            void SaveLayer(LayerType type, object? value, int count)
            {
                Debug($"SaveLayer({type}, {count})");

                layersStack.Push((sb, type, value));
                sb = new StringBuilder();

                Save(count);
            }

            void RestoreLayer()
            {
                // layers

                var layer = layersStack.Count > 0 ? layersStack.Pop() : null;
                if (layer is null)
                {
                    return;
                }

                var (builder, type, value) = layer.Value;
                var content = sb.ToString();

                sb = builder;

                Debug($"StartLayer({type})");

                switch (type)
                {
                    case LayerType.UnknownPaint:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append(content);

                        break;
                    }
                    case LayerType.MaskGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append($"<DrawingGroup>{NewLine}");
                        sb.Append(content);
                        sb.Append($"</DrawingGroup>{NewLine}");

                        break;
                    }
                    case LayerType.MaskBrush:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append($"<DrawingGroup.OpacityMask>{NewLine}");
                        sb.Append($"  <VisualBrush");
                        sb.Append($" TileMode=\"None\"");
                        sb.Append($" Stretch=\"None\"");

                        if (UseCompatMode)
                        {
                            // sb.Append($" Viewport=\"{ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                            // sb.Append($" Viewbox=\"{ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
                        }
                        else
                        {
                            // sb.Append($" SourceRect=\"{ToRect(sourceRect)}\"");
                            // sb.Append($" DestinationRect=\"{ToRect(destinationRect)}\"");
                        }

                        sb.Append($">{NewLine}");
                        sb.Append($"    <VisualBrush.Visual>{NewLine}");
                        sb.Append($"      <Image>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"      <Image.Source>{NewLine}");
                        }

                        sb.Append($"        <DrawingImage>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"        <DrawingImage.Drawing>{NewLine}");
                        }

                        sb.Append($"          <DrawingGroup>{NewLine}");
                        sb.Append(content);
                        sb.Append($"          </DrawingGroup>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"        </DrawingImage.Drawing>{NewLine}");
                        }

                        sb.Append($"        </DrawingImage>{NewLine}");

                        if (UseCompatMode)
                        {
                            sb.Append($"      </Image.Source>{NewLine}");
                        }

                        sb.Append($"      </Image>{NewLine}");
                        sb.Append($"    </VisualBrush.Visual>{NewLine}");
                        sb.Append($"  </VisualBrush>{NewLine}");
                        sb.Append($"</DrawingGroup.OpacityMask>{NewLine}");

                        break;
                    }
                    case LayerType.OpacityGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint paint)
                        {
                            break;
                        }

                        if (paint.Color is { } skColor)
                        {
                            sb.Append($"<DrawingGroup Opacity=\"{XamlConverter.ToString(skColor.Alpha / 255.0)}\">{NewLine}");
                            sb.Append(content);
                            sb.Append($"</DrawingGroup>{NewLine}");
                        }

                        break;
                    }
                    case LayerType.FilterGroup:
                    {
                        if (value is not ShimSkiaSharp.SKPaint)
                        {
                            break;
                        }

                        sb.Append(content);

                        break;
                    }
                }

                Debug($"EndLayer({type})");
            }

            void SaveGroups()
            {
                // matrix

                totalMatrixStack.Push(currentTotalMatrix);
                currentTotalMatrix = default;

                // clip-path

                clipPathStack.Push(currentClipPath);
                currentClipPath = default;
            }

            void RestoreGroups()
            {
                // clip-path

                if (currentClipPath is { })
                {
                    sb.Append($"</DrawingGroup>{NewLine}");

                    Debug($"EndClipPath({clipPathStack.Count})");
                }

                currentClipPath = default;

                if (clipPathStack.Count > 0)
                {
                    currentClipPath = clipPathStack.Pop();
                }

                // matrix

                if (currentTotalMatrix is { })
                {
                    sb.Append($"</DrawingGroup>{NewLine}");

                    Debug($"EndMatrix({totalMatrixStack.Count})");
                }

                currentTotalMatrix = default;

                if (totalMatrixStack.Count > 0)
                {
                    currentTotalMatrix = totalMatrixStack.Pop();
                }
            }

            void Save(int count)
            {
                Debug($"Save({count})");
                SaveGroups();
            }

            void Restore(int count)
            {
                Debug($"Restore({count})");
                RestoreLayer();
                RestoreGroups();
            }

            RestoreGroups();

            sb.Append($"</DrawingGroup>");

            return sb.ToString(); 
            */      
        }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup{XamlConverter.ToKey(Key)}");

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Opacity != 1.0)
            {
                sb.Append($" Opacity=\"{XamlConverter.ToString(Opacity)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (ClipGeometry is { })
            {
                var clipGeometry = XamlConverter.ToSvgPathData(ClipGeometry);

                sb.Append($"  <DrawingGroup.ClipGeometry>{context.NewLine}");
                sb.Append($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{context.NewLine}");
                sb.Append($"  </DrawingGroup.ClipGeometry>{context.NewLine}");
            }

            if (Transform is { })
            {
                var matrix = Transform.Value;

                sb.Append($"  <DrawingGroup.Transform>{context.NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(matrix)}\"/>{context.NewLine}");
                sb.Append($"  </DrawingGroup.Transform>{context.NewLine}");
            }

            if (OpacityMask is { })
            {
                sb.Append($"<DrawingGroup.OpacityMask>{context.NewLine}");
                sb.Append($"  <VisualBrush");
                sb.Append($" TileMode=\"None\"");
                sb.Append($" Stretch=\"None\"");

                // TODO:
                // if (context.UseCompatMode)
                // {
                //     sb.Append($" Viewport=\"{XamlConverter.ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                //     sb.Append($" Viewbox=\"{XamlConverter.ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
                // }
                // else
                // {
                //     sb.Append($" SourceRect=\"{XamlConverter.ToRect(sourceRect)}\"");
                //     sb.Append($" DestinationRect=\"{XamlConverter.ToRect(destinationRect)}\"");
                // }

                sb.Append($">{context.NewLine}");

                sb.Append($"    <VisualBrush.Visual>{context.NewLine}");
                sb.Append($"      <Image>{context.NewLine}");

                if (context.UseCompatMode)
                {
                    sb.Append($"      <Image.Source>{context.NewLine}");
                }

                sb.Append($"        <DrawingImage>{context.NewLine}");

                if (context.UseCompatMode)
                {
                    sb.Append($"        <DrawingImage.Drawing>{context.NewLine}");
                }

                sb.Append($"          <DrawingGroup>{context.NewLine}");

                // TODO: OpacityMask -> sb.Append(content);

                sb.Append($"          </DrawingGroup>{context.NewLine}");

                if (context.UseCompatMode)
                {
                    sb.Append($"        </DrawingImage.Drawing>{context.NewLine}");
                }

                sb.Append($"        </DrawingImage>{context.NewLine}");

                if (context.UseCompatMode)
                {
                    sb.Append($"      </Image.Source>{context.NewLine}");
                }

                sb.Append($"      </Image>{context.NewLine}");
                sb.Append($"    </VisualBrush.Visual>{context.NewLine}");
                sb.Append($"  </VisualBrush>{context.NewLine}");
                sb.Append($"</DrawingGroup.OpacityMask>{context.NewLine}");
            }

            sb.Append($"</DrawingGroup>");

            return sb.ToString();
        }
    }

    public record DrawingImage : Drawing
    {
        public Drawing? Drawing { get; }

        public DrawingImage(Drawing? drawing)
        {
            Drawing = drawing;
        }

        public override string Generate(GeneratorContext context)
        {
            if (Drawing is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<DrawingImage>{context.NewLine}");

            if (context.UseCompatMode)
            {
                sb.Append($"  <DrawingImage.Drawing>{context.NewLine}");
            }

            sb.Append(Drawing.Generate(context));

            if (context.UseCompatMode)
            {
                sb.Append($"  </DrawingImage.Drawing>{context.NewLine}");
            }

            sb.Append($"</DrawingImage>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record Image : Resource
    {
        public DrawingImage? Source { get; }

        public Image(DrawingImage? source)
        {
            Source = source;
        }

        public override string Generate(GeneratorContext context)
        {
            if (Source is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<Image{XamlConverter.ToKey(Key)}");

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0) && context.WriteResources)
            {
                // sb.Append(context.UseCompatMode
                //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                //     : $" xmlns=\"https://github.com/avaloniaui\"");
                // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0) && context.WriteResources)
            {
                sb.Append($"<Image.Resources>{context.NewLine}");
                sb.Append(context.Resources.Generate(context));
                sb.Append($"</Image.Resources>{context.NewLine}");
            }

            if (context.UseCompatMode)
            {
                sb.Append($"<Image.Source>{context.NewLine}");
            }

            sb.Append(Source.Generate(context));

            if (context.UseCompatMode)
            {
                sb.Append($"</Image.Source>{context.NewLine}");
            }

            sb.Append($"</Image>");

            return sb.ToString();
        }
    }
}
