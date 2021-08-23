namespace SvgToXamlConverter
{
    public abstract record Resource : IGenerator
    {
        public string? Key { get; init; }

        public abstract string Generate(GeneratorContext context);
    }
}
