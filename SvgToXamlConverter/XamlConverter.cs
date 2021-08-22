using System.Globalization;
using System.Text;

namespace SvgToXamlConverter
{
    public static class XamlConverter
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

        public static string ToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToString(float value)
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
            sb.Append(ToString(skPoint.X));
            sb.Append(',');
            sb.Append(ToString(skPoint.Y));
            return sb.ToString();
        }

        public static string ToRect(ShimSkiaSharp.SKRect sKRect)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(sKRect.Left));
            sb.Append(',');
            sb.Append(ToString(sKRect.Top));
            sb.Append(',');
            sb.Append(ToString(sKRect.Width));
            sb.Append(',');
            sb.Append(ToString(sKRect.Height));
            return sb.ToString();
        }

        public static string ToMatrix(SkiaSharp.SKMatrix skMatrix)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skMatrix.ScaleX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.ScaleY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransY));
            return sb.ToString();
        }

        public static string ToMatrix(ShimSkiaSharp.SKMatrix skMatrix)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skMatrix.ScaleX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.ScaleY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransY));
            return sb.ToString();
        }

        public static string ToSvgPathData(SkiaSharp.SKPath path)
        {
            if (path.FillType == SkiaSharp.SKPathFillType.EvenOdd)
            {
                // EvenOdd
                var sb = new StringBuilder();
                sb.Append("F0 ");
                sb.Append(path.ToSvgPathData());
                return sb.ToString();
            }
            else
            {
                // Nonzero 
                var sb = new StringBuilder();
                sb.Append("F1 ");
                sb.Append(path.ToSvgPathData());
                return sb.ToString();
            }
        }
    }
}
