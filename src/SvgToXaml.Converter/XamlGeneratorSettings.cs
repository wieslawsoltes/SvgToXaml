using SvgToXaml.Model.Resources;

namespace SvgToXaml.Converter;

public enum GeneratorType
{
    Drawing,
    Canvas
}

public record XamlGeneratorSettings
{
    public string NewLine { get; init; } = "\r\n";

    public bool UseCompatMode { get; init; } = false;

    public bool AddTransparentBackground { get; init; } = false;

    public bool ReuseExistingResources { get; init; } = false;

    public bool TransformGeometry { get; init; } = false;

    public bool WriteResources { get; init; } = false;

    public GeneratorType GeneratorType { get; init; } = GeneratorType.Drawing;

    public ResourceDictionary? Resources { get; init; }
}
