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

        public HashSet<Brush> UseBrushes { get; init; } = new();

        public HashSet<Pen> UsePens { get; init; } = new();

        public string Generate(GeneratorContext context)
        {
            var sb = new StringBuilder();

            if (context.ReuseExistingResources)
            {
                foreach (var resource in UseBrushes)
                {
                    sb.Append(resource.Generate(context));
                }

                foreach (var resource in UsePens)
                {
                    sb.Append(resource.Generate(context));
                }
            }
            else
            {
                 foreach (var resource in Brushes)
                 {
                     sb.Append(resource.Value.Brush.Generate(context));
                 }

                 foreach (var resource in Pens)
                 {
                     sb.Append(resource.Value.Pen.Generate(context));
                 }               
            }

            return sb.ToString();
        }
    }
}
