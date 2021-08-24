namespace SvgToXamlConverter
{
    public record Dashes
    {
        public float[]? Intervals { get; init; }

        public float Phase { get; init; }
    }
}
