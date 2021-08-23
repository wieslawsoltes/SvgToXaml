using System.Text;

namespace SvgToXamlConverter
{
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
}
