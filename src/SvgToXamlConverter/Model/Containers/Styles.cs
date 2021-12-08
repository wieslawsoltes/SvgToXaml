using System.Collections.Generic;
using SvgToXamlConverter.Model.Resources;

namespace SvgToXamlConverter.Model.Containers;

public record Styles
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
}