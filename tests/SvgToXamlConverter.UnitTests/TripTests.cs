using System.Collections.Generic;
using System.Linq;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using SvgToXaml.Model;
using Xunit;
using Xunit.Abstractions;

namespace SvgToXamlConverter.UnitTests;

public class TripTests
{
    private readonly ITestOutputHelper _output;

    public TripTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [AvaloniaFact]
    public void Should_Convert_Svg_To_Xaml_Drawing_And_Load()
    {
        var svg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"" viewBox=""0 0 100 100"">
  <circle cx=""50"" cy=""50"" r=""40"" stroke=""green"" stroke-width=""4"" fill=""yellow"" />
</svg>";

        var converter = new SvgToXaml.Converter.SvgToXamlConverter
        {
            GeneratorType = SvgToXaml.Converter.GeneratorType.Drawing
        };
        var inputItems = new List<InputItem>
        {
            new InputItem("TestCircle", svg)
        };

        var xaml = converter.ToXamlStyles(inputItems);

        _output.WriteLine(svg);
        _output.WriteLine(xaml);

        Assert.NotNull(xaml);
        Assert.NotEmpty(xaml);

        var loaded = AvaloniaRuntimeXamlLoader.Load(xaml);
        Assert.NotNull(loaded);
    }

    [AvaloniaFact]
    public void Should_Convert_Svg_To_Xaml_Canvas_And_Load()
    {
        var svg = @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""100"" viewBox=""0 0 100 100"">
  <rect x=""10"" y=""10"" width=""80"" height=""80"" style=""fill:blue;stroke:pink;stroke-width:5;fill-opacity:0.1;stroke-opacity:0.9"" />
</svg>";

        var converter = new SvgToXaml.Converter.SvgToXamlConverter
        {
            GeneratorType = SvgToXaml.Converter.GeneratorType.Canvas
        };
        var inputItems = new List<InputItem>
        {
            new InputItem("TestRect", svg)
        };

        var xaml = converter.ToXamlStyles(inputItems);

        _output.WriteLine(svg);
        _output.WriteLine(xaml);

        Assert.NotNull(xaml);
        Assert.NotEmpty(xaml);

        var loaded = AvaloniaRuntimeXamlLoader.Load(xaml);
        Assert.NotNull(loaded);
    }

    [AvaloniaFact]
    public void Should_Convert_Complex_Svg_To_Xaml_Drawing_And_Load()
    {
        var svg = @"<svg height=""100"" width=""100"" xmlns=""http://www.w3.org/2000/svg"">
  <g fill=""none"" stroke=""black"">
    <path stroke-width=""2"" d=""M5 20 l215 0"" />
    <path stroke-width=""4"" d=""M5 40 l215 0"" />
    <path stroke-width=""9"" d=""M5 60 l215 0"" />
  </g>
</svg>";

        var converter = new SvgToXaml.Converter.SvgToXamlConverter
        {
            GeneratorType = SvgToXaml.Converter.GeneratorType.Drawing
        };
        var inputItems = new List<InputItem>
        {
            new InputItem("TestComplex", svg)
        };

        var xaml = converter.ToXamlStyles(inputItems);

        _output.WriteLine(svg);
        _output.WriteLine(xaml);

        Assert.NotNull(xaml);
        Assert.NotEmpty(xaml);

        var loaded = AvaloniaRuntimeXamlLoader.Load(xaml);
        Assert.NotNull(loaded);
    }

    [AvaloniaFact]
    public void Should_Convert_Complex_Svg_To_Xaml_Canvas_And_Load()
    {
        var svg = @"<svg height=""100"" width=""100"" xmlns=""http://www.w3.org/2000/svg"">
  <g fill=""none"" stroke=""black"">
    <path stroke-width=""2"" d=""M5 20 l215 0"" />
    <path stroke-width=""4"" d=""M5 40 l215 0"" />
    <path stroke-width=""9"" d=""M5 60 l215 0"" />
  </g>
</svg>";

        var converter = new SvgToXaml.Converter.SvgToXamlConverter
        {
            GeneratorType = SvgToXaml.Converter.GeneratorType.Canvas
        };
        var inputItems = new List<InputItem>
        {
            new InputItem("TestComplex", svg)
        };

        var xaml = converter.ToXamlStyles(inputItems);

        _output.WriteLine(svg);
        _output.WriteLine(xaml);

        Assert.NotNull(xaml);
        Assert.NotEmpty(xaml);

        var loaded = AvaloniaRuntimeXamlLoader.Load(xaml);
        Assert.NotNull(loaded);
    }
}
