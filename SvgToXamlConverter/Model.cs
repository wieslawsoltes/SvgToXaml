using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvgToXamlConverter
{
    public record GeneratorContext
    {
        public string NewLine { get; set; } = "\r\n";

        public bool UseCompatMode { get; set; } = false;

        public bool UseBrushTransform { get; set; } = false;
    }

    public interface IGenerator
    {
        string Generate(GeneratorContext context);
    }

    public abstract record Resource : IGenerator
    {
        public string? Key { get; set; }

        public abstract string Generate(GeneratorContext context);
    }

    public record Point(double X, double Y);

    public abstract record Brush : Resource
    {
        public SkiaSharp.SKRect Bounds { get; set; }

        public SkiaSharp.SKMatrix? LocalMatrix { get; set; }
  
        protected SkiaSharp.SKMatrix WithTransXY(SkiaSharp.SKMatrix matrix, float x, float y)
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
        public ShimSkiaSharp.SKColor Color { get; set; } 

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
        public float Offset { get; set; }

        public ShimSkiaSharp.SKColor Color { get; set; } 

        public override string Generate(GeneratorContext context)
        {
            return $"<GradientStop Offset=\"{XamlConverter.ToString(Offset)}\" Color=\"{XamlConverter.ToHexColor(Color)}\"/>{context.NewLine}";
        }
    }

    public abstract record GradientBrush : Brush
    {
        public ShimSkiaSharp.SKShaderTileMode Mode { get; set; }

        public List<GradientStop> GradientStops { get; set; }

        public GradientBrush()
        {
            GradientStops = new List<GradientStop>();
        }
    }

    public record LinearGradientBrush : GradientBrush
    {
        public SkiaSharp.SKPoint Start { get; set; }

        public SkiaSharp.SKPoint End { get; set; }

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
        public SkiaSharp.SKPoint Center { get; set; }

        public float Radius { get; set; }

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
        public float StartRadius { get; set; }

        public float EndRadius { get; set; }

        public SkiaSharp.SKPoint Start { get; set; }

        public SkiaSharp.SKPoint End { get; set; }

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
        public Image? Picture { get; set; }

        public ShimSkiaSharp.SKRect CullRect { get; set; }

        public ShimSkiaSharp.SKRect Tile { get; set; }

        public ShimSkiaSharp.SKShaderTileMode TileMode { get; set; }
 
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
        public float[]? Intervals { get; set; }

        public float Phase { get; set; }
    }
    
    public record Pen : Resource
    {
        public SkiaSharp.SKRect Bounds { get; set; }

        public Brush? Brush { get; set; }

        public float StrokeWidth { get; set; }

        public ShimSkiaSharp.SKStrokeCap StrokeCap { get; set; }

        public ShimSkiaSharp.SKStrokeJoin StrokeJoin { get; set; }

        public float StrokeMiter { get; set; }

        public Dashes? Dashes { get; set; }

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
        public SkiaSharp.SKPath? Geometry { get; set; }

        public Brush? Brush { get; set; }

        public Pen? Pen { get; set; }

        public override string Generate(GeneratorContext context)
        {
            throw new NotImplementedException();
        }
    }

    public record DrawingGroup : Drawing
    {
        public double Opacity { get; set; }

        public SkiaSharp.SKMatrix? Transform { get; set; }

        public SkiaSharp.SKPath? ClipGeometry { get; set; }

        public Brush? OpacityMask { get; set; }

        public List<Drawing> Children { get; set; }

        public DrawingGroup()
        {
            Children = new List<Drawing>();
        }

        public override string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            sb.Append($"<DrawingGroup>");

            if (Key is { })
            {
                sb.Append($"{XamlConverter.ToKey(Key)}");
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Opacity != 1.0)
            {
                sb.Append($" Opacity=\"{XamlConverter.ToString(Opacity)}\"");
            }

            sb.Append($">{context.NewLine}");

            if (OpacityMask is { })
            {
                sb.Append($"<DrawingGroup.OpacityMask>{context.NewLine}");
                sb.Append($"  <VisualBrush");
                sb.Append($" TileMode=\"None\"");
                sb.Append($" Stretch=\"None\"");

                if (context.UseCompatMode)
                {
                    // sb.Append($" Viewport=\"{ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
                    // sb.Append($" Viewbox=\"{ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
                }
                else
                {
                    // sb.Append($" SourceRect=\"{ToRect(sourceRect)}\"");
                    // sb.Append($" DestinationRect=\"{ToRect(destinationRect)}\"");
                }

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
                sb.Append(OpacityMask.Generate(context));
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

                sb.Append($"<DrawingGroup>{context.NewLine}");
                sb.Append($"  <DrawingGroup.Transform>{context.NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{XamlConverter.ToMatrix(matrix)}\"/>{context.NewLine}");
                sb.Append($"  </DrawingGroup.Transform>{context.NewLine}");
            }

            foreach (var drawing in Children)
            {
                sb.Append(drawing.Generate(context));
            }

            sb.Append($"</DrawingGroup>{context.NewLine}");

            return sb.ToString();
        }
    }

    public record ImageDrawing : Drawing
    {
        public override string Generate(GeneratorContext context)
        {
            throw new NotImplementedException();
        }
    }

    public record Image : Resource
    {
        public ImageDrawing? Source { get; set; }

        public override string Generate(GeneratorContext context)
        {
            throw new NotImplementedException();
        }
    }
}
