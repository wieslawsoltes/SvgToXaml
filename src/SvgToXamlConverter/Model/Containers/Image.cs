using SvgToXamlConverter.Model.Drawing;
using SvgToXamlConverter.Model.Resources;

namespace SvgToXamlConverter.Model.Containers
{
    public record Image : Resource
    {
        public DrawingImage? Source { get; }

        public Image(DrawingImage? source = null, string? key = null)
        {
            Key = key;
            Source = source;
        }
    }
}
