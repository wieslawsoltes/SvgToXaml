using System.Collections.Generic;
using System.Text;

namespace SvgToXamlConverter
{
    public record Styles : IGenerator
    {
        public bool GenerateImage { get; init; }

        public bool GeneratePreview { get; init; }

        public List<Resource>? Resources { get; init; }

        public Styles(List<Resource>? resources, bool generateImage = false, bool generatePreview = true)
        {
            GenerateImage = generateImage;
            GeneratePreview = generatePreview;
            Resources = resources;
        }

        public string Generate(GeneratorContext context)
        {
            if (Resources is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            var content = new StringBuilder();

            foreach (var result in Resources)
            {
                content.Append(result?.Generate(context with { WriteResources = false }));
                content.Append(context.NewLine);
            }

            if (context.UseCompatMode)
            {
                sb.Append($"<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"{context.NewLine}");
                sb.Append($"                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{context.NewLine}");
            }
            else
            {
                sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{context.NewLine}");
                sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{context.NewLine}");
            }

            if (GeneratePreview && !context.UseCompatMode)
            {
                sb.Append($"  <Design.PreviewWith>{context.NewLine}");
                sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">{context.NewLine}");
                sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">{context.NewLine}");

                foreach (var result in Resources)
                {
                    if (GenerateImage)
                    {
                        sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>{context.NewLine}");
                    }
                    else
                    {
                        sb.Append($"        <Image>{context.NewLine}");

                        if (context.UseCompatMode)
                        {
                            sb.Append($"            <Image.Source>{context.NewLine}");
                        }

                        sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>{context.NewLine}");

                        if (context.UseCompatMode)
                        {
                            sb.Append($"            </Image.Source>{context.NewLine}");
                        }

                        sb.Append($"        </Image>{context.NewLine}");
                    }
                }

                sb.Append($"      </WrapPanel>{context.NewLine}");
                sb.Append($"    </ScrollViewer>{context.NewLine}");
                sb.Append($"  </Design.PreviewWith>{context.NewLine}");
            }

            if (!context.UseCompatMode)
            {
                sb.Append($"  <Style>{context.NewLine}");
                sb.Append($"    <Style.Resources>{context.NewLine}");
            }

            if (context.Resources is { } && (context.Resources.Brushes.Count > 0 || context.Resources.Pens.Count > 0))
            {
                sb.Append(context.Resources.Generate(context with { WriteResources = false }));
            }

            sb.Append(content);

            if (context.UseCompatMode)
            {
                sb.Append($"</ResourceDictionary>");
            }
            else
            {
                sb.Append($"    </Style.Resources>{context.NewLine}");
                sb.Append($"  </Style>{context.NewLine}");
                sb.Append($"</Styles>{context.NewLine}");
            }

            return sb.ToString();
        }
    }
}
