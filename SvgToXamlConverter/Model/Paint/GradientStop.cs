namespace SvgToXamlConverter
{
    public record GradientStop : Resource
    {
        public float Offset { get; init; }

        public ShimSkiaSharp.SKColor Color { get; init; } 

        public override string Generate(GeneratorContext context)
        {
            return $"<GradientStop Offset=\"{XamlConverter.ToString(Offset)}\" Color=\"{XamlConverter.ToHexColor(Color)}\"/>{context.NewLine}";
        }
    }
}
