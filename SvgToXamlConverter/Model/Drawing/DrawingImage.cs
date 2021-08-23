using System.Text;

namespace SvgToXamlConverter
{
    public record DrawingImage : Drawing
    {
        public Drawing? Drawing { get; }

        public DrawingImage(Drawing? drawing = null)
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
}
