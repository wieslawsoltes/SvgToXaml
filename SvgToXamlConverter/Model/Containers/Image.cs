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

        public override string Generate(GeneratorContext context)
        {
            if (Source is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<Image{XamlConverter.ToKey(Key)}");

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0) && context.WriteResources)
            {
                // sb.Append(context.UseCompatMode
                //     ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                //     : $" xmlns=\"https://github.com/avaloniaui\"");
                // sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
            }

            sb.Append($">{context.NewLine}");

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0) && context.WriteResources)
            {
                sb.Append($"<Image.Resources>{context.NewLine}");
                sb.Append(context.Resources.Generate(context));
                sb.Append($"</Image.Resources>{context.NewLine}");
            }

            if (context.UseCompatMode)
            {
                sb.Append($"<Image.Source>{context.NewLine}");
            }

            sb.Append(Source.Generate(context));

            if (context.UseCompatMode)
            {
                sb.Append($"</Image.Source>{context.NewLine}");
            }

            sb.Append($"</Image>");

            return sb.ToString();
        }
    }
}
