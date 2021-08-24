using System.Text;

namespace SvgToXamlConverter
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
