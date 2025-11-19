using System.Globalization;
using System.Text;

namespace SvgToXaml.Converter;

public static class GeneratorHelper
{
    public static string ToKey(string? key)
    {
        return key is null ? "" : $" x:Key=\"{key}\"";
    }

    public static string ToGradientSpreadMethod(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
    {
        return shaderTileMode switch
        {
            ShimSkiaSharp.SKShaderTileMode.Clamp => "Pad",
            ShimSkiaSharp.SKShaderTileMode.Repeat => "Repeat",
            ShimSkiaSharp.SKShaderTileMode.Mirror => "Reflect",
            _ => "Pad"
        };
    }

    public static string ToTileMode(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
    {
        return shaderTileMode switch
        {
            ShimSkiaSharp.SKShaderTileMode.Clamp => "None",
            ShimSkiaSharp.SKShaderTileMode.Repeat => "Tile",
            ShimSkiaSharp.SKShaderTileMode.Mirror => "FlipXY",
            _ => "None"
        };
    }

    public static string ToPenLineCap(ShimSkiaSharp.SKStrokeCap strokeCap)
    {
        return strokeCap switch
        {
            ShimSkiaSharp.SKStrokeCap.Butt => "Flat",
            ShimSkiaSharp.SKStrokeCap.Round => "Round",
            ShimSkiaSharp.SKStrokeCap.Square => "Square",
            _ => "Flat"
        };
    }

    public static string ToPenLineJoin(ShimSkiaSharp.SKStrokeJoin strokeJoin, bool useCompatMode = false)
    {
        return strokeJoin switch
        {
            ShimSkiaSharp.SKStrokeJoin.Miter => "Miter",
            ShimSkiaSharp.SKStrokeJoin.Round => "Round",
            ShimSkiaSharp.SKStrokeJoin.Bevel => "Bevel",
            _ => useCompatMode ? "Miter" : "Bevel"
        };
    }

    public static string ToXamlString(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string ToXamlString(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string ToHexColor(ShimSkiaSharp.SKColor skColor)
    {
        var sb = new StringBuilder();
        sb.Append('#');
        sb.AppendFormat("{0:X2}", skColor.Alpha);
        sb.AppendFormat("{0:X2}", skColor.Red);
        sb.AppendFormat("{0:X2}", skColor.Green);
        sb.AppendFormat("{0:X2}", skColor.Blue);
        return sb.ToString();
    }

    public static string ToPoint(SkiaSharp.SKPoint skPoint)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(skPoint.X));
        sb.Append(',');
        sb.Append(ToXamlString(skPoint.Y));
        return sb.ToString();
    }

    public static string ToRect(ShimSkiaSharp.SKRect sKRect)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(sKRect.Left));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Top));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Width));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Height));
        return sb.ToString();
    }

    public static string ToRect(SkiaSharp.SKRect sKRect)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(sKRect.Left));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Top));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Width));
        sb.Append(',');
        sb.Append(ToXamlString(sKRect.Height));
        return sb.ToString();
    }

    public static string ToMatrix(SkiaSharp.SKMatrix skMatrix)
    {
        var sb = new StringBuilder();
        sb.Append(ToXamlString(skMatrix.ScaleX));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.SkewY));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.SkewX));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.ScaleY));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.TransX));
        sb.Append(',');
        sb.Append(ToXamlString(skMatrix.TransY));
        return sb.ToString();
    }

    public static string ToSvgPathData(SkiaSharp.SKPath path, SkiaSharp.SKMatrix matrix)
    {
        var transformedPath = new SkiaSharp.SKPath(path);
        transformedPath.Transform(matrix);
        if (transformedPath.FillType == SkiaSharp.SKPathFillType.EvenOdd)
        {
            // EvenOdd
            var sb = new StringBuilder();
            sb.Append("F0 ");
            sb.Append(transformedPath.ToSvgPathData());
            return sb.ToString();
        }
        else
        {
            // Nonzero 
            var sb = new StringBuilder();
            sb.Append("F1 ");
            sb.Append(transformedPath.ToSvgPathData());
            return sb.ToString();
        }
    }
}
