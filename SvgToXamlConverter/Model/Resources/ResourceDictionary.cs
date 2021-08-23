using System.Collections.Generic;
using System.Text;

namespace SvgToXamlConverter
{
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
}
