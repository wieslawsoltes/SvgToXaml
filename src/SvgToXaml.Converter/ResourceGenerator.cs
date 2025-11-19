using System;
using System.Text;
using SvgToXaml.Model.Containers;
using SvgToXaml.Model.Resources;

namespace SvgToXaml.Converter;

public static class ResourceGenerator
{
    public static string GenerateResourceDictionary(ResourceDictionary resourceDictionary, XamlGeneratorSettings settings, Func<Image, XamlGeneratorSettings, SkiaSharp.SKMatrix?, string> generateImage)
    {
        var sb = new StringBuilder();

        if (settings.ReuseExistingResources)
        {
            foreach (var resource in resourceDictionary.UseBrushes)
            {
                sb.Append(BrushGenerator.GenerateBrush(resource, settings, generateImage));
            }

            foreach (var resource in resourceDictionary.UsePens)
            {
                sb.Append(PenGenerator.GeneratePen(resource, settings, generateImage));
            }
        }
        else
        {
            foreach (var resource in resourceDictionary.Brushes)
            {
                sb.Append(BrushGenerator.GenerateBrush(resource.Value.Brush, settings, generateImage));
            }

            foreach (var resource in resourceDictionary.Pens)
            {
                sb.Append(PenGenerator.GeneratePen(resource.Value.Pen, settings, generateImage));
            }               
        }

        return sb.ToString();
    }

    public static string GenerateStyles(Styles styles, XamlGeneratorSettings settings, Func<Resource, XamlGeneratorSettings, SkiaSharp.SKMatrix?, string> generateResource, Func<ResourceDictionary, XamlGeneratorSettings, string> generateResourceDictionary)
    {
        if (styles.Resources is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        var content = new StringBuilder();

        foreach (var result in styles.Resources)
        {
            content.Append(generateResource(result, settings with { WriteResources = false, AddTransparentBackground = false }, SkiaSharp.SKMatrix.CreateIdentity()));
            content.Append(settings.NewLine);
        }

        if (settings.UseCompatMode)
        {
            sb.Append($"<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"{settings.NewLine}");
            sb.Append($"                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{settings.NewLine}");
        }
        else
        {
            sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{settings.NewLine}");
            sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{settings.NewLine}");
        }

        if (styles.GeneratePreview && !settings.UseCompatMode)
        {
            sb.Append($"  <Design.PreviewWith>{settings.NewLine}");
            sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">{settings.NewLine}");
            sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">{settings.NewLine}");

            foreach (var result in styles.Resources)
            {
                if (styles.GenerateImage)
                {
                    sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>{settings.NewLine}");
                }
                else
                {
                    sb.Append($"        <Image>{settings.NewLine}");

                    if (settings.UseCompatMode)
                    {
                        sb.Append($"            <Image.Source>{settings.NewLine}");
                    }

                    sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>{settings.NewLine}");

                    if (settings.UseCompatMode)
                    {
                        sb.Append($"            </Image.Source>{settings.NewLine}");
                    }

                    sb.Append($"        </Image>{settings.NewLine}");
                }
            }

            sb.Append($"      </WrapPanel>{settings.NewLine}");
            sb.Append($"    </ScrollViewer>{settings.NewLine}");
            sb.Append($"  </Design.PreviewWith>{settings.NewLine}");
        }

        if (!settings.UseCompatMode)
        {
            sb.Append($"  <Style>{settings.NewLine}");
            sb.Append($"    <Style.Resources>{settings.NewLine}");
        }

        if (settings.Resources is { } && (settings.Resources.Brushes.Count > 0 || settings.Resources.Pens.Count > 0))
        {
            sb.Append(generateResourceDictionary(settings.Resources, settings with { WriteResources = false, AddTransparentBackground = false }));
        }

        sb.Append(content);

        if (settings.UseCompatMode)
        {
            sb.Append($"</ResourceDictionary>");
        }
        else
        {
            sb.Append($"    </Style.Resources>{settings.NewLine}");
            sb.Append($"  </Style>{settings.NewLine}");
            sb.Append($"</Styles>{settings.NewLine}");
        }

        return sb.ToString();
    }
}
