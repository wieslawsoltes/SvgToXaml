using SvgToXaml.Model.Drawing;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Model.Containers;

public record Image : Resource
{
    public DrawingImage? Source { get; }

    public Image(DrawingImage? source = null, string? key = null)
    {
        Key = key;
        Source = source;
    }
}
