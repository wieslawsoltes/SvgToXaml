using System;
using System.Text;

namespace SvgToXamlConverter
{
    public record TwoPointConicalGradientBrush : GradientBrush
    {
        public float StartRadius { get; init; }

        public float EndRadius { get; init; }

        public SkiaSharp.SKPoint Start { get; init; }

        public SkiaSharp.SKPoint End { get; init; }
    }
}
