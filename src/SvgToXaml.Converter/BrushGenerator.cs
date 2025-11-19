using System;
using System.Text;
using SvgToXaml.Model.Containers;
using SvgToXaml.Model.Paint;

namespace SvgToXaml.Converter;

public static class BrushGenerator
{
    public static string GenerateBrush(Brush brush, XamlGeneratorSettings settings, Func<Image, XamlGeneratorSettings, SkiaSharp.SKMatrix?, string> generateImage)
    {
        return brush switch
        {
            SolidColorBrush solidColorBrush => GenerateSolidColorBrush(solidColorBrush, settings),
            LinearGradientBrush linearGradientBrush => GenerateLinearGradientBrush(linearGradientBrush, settings),
            RadialGradientBrush radialGradientBrush => GenerateRadialGradientBrush(radialGradientBrush, settings),
            TwoPointConicalGradientBrush twoPointConicalGradientBrush => GenerateTwoPointConicalGradientBrush(twoPointConicalGradientBrush, settings),
            PictureBrush pictureBrush => GeneratePictureBrush(pictureBrush, settings, generateImage),
            _ => ""
        };
    }

    public static string GenerateSolidColorBrush(SolidColorBrush solidColorBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        sb.Append($"<SolidColorBrush{GeneratorHelper.ToKey(solidColorBrush.Key)}");
        sb.Append($" Color=\"{GeneratorHelper.ToHexColor(solidColorBrush.Color)}\"");
        sb.Append($"/>{settings.NewLine}");

        return sb.ToString();
    }

    public static string GenerateLinearGradientBrush(LinearGradientBrush linearGradientBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        var start = linearGradientBrush.Start;
        var end = linearGradientBrush.End;

        sb.Append($"<LinearGradientBrush{GeneratorHelper.ToKey(linearGradientBrush.Key)}");

        sb.Append($" StartPoint=\"{GeneratorHelper.ToPoint(start)}\"");
        sb.Append($" EndPoint=\"{GeneratorHelper.ToPoint(end)}\"");

        if (linearGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" SpreadMethod=\"{GeneratorHelper.ToGradientSpreadMethod(linearGradientBrush.Mode)}\"");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($" MappingMode=\"Absolute\"");
        }

        sb.Append($">{settings.NewLine}");

        if (linearGradientBrush.LocalMatrix is { })
        {
            var localMatrix = linearGradientBrush.LocalMatrix.Value;

            sb.Append($"  <LinearGradientBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{GeneratorHelper.ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </LinearGradientBrush.Transform>{settings.NewLine}");
        }

        if (linearGradientBrush.GradientStops.Count > 0)
        {
            sb.Append($"  <LinearGradientBrush.GradientStops>{settings.NewLine}");

            foreach (var stop in linearGradientBrush.GradientStops)
            {
                var color = GeneratorHelper.ToHexColor(stop.Color);
                var offset = GeneratorHelper.ToXamlString(stop.Offset);
                sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{settings.NewLine}");
            }

            sb.Append($"  </LinearGradientBrush.GradientStops>{settings.NewLine}");
        }

        sb.Append($"</LinearGradientBrush>{settings.NewLine}");

        return sb.ToString();
    }

    public static string GenerateRadialGradientBrush(RadialGradientBrush radialGradientBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        var radius = radialGradientBrush.Radius;

        var center = radialGradientBrush.Center;
        var gradientOrigin = radialGradientBrush.Center;

        if (!settings.UseCompatMode)
        {
            radius = radius / radialGradientBrush.Bounds.Width;
        }

        sb.Append($"<RadialGradientBrush{GeneratorHelper.ToKey(radialGradientBrush.Key)}");

        sb.Append($" Center=\"{GeneratorHelper.ToPoint(center)}\"");
        sb.Append($" GradientOrigin=\"{GeneratorHelper.ToPoint(gradientOrigin)}\"");

        if (settings.UseCompatMode)
        {
            sb.Append($" RadiusX=\"{GeneratorHelper.ToXamlString(radius)}\"");
            sb.Append($" RadiusY=\"{GeneratorHelper.ToXamlString(radius)}\"");
        }
        else
        {
            sb.Append($" Radius=\"{GeneratorHelper.ToXamlString(radius)}\"");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($" MappingMode=\"Absolute\"");
        }

        if (radialGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" SpreadMethod=\"{GeneratorHelper.ToGradientSpreadMethod(radialGradientBrush.Mode)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (radialGradientBrush.LocalMatrix is { })
        {
            var localMatrix = radialGradientBrush.LocalMatrix.Value;

            sb.Append($"  <RadialGradientBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{GeneratorHelper.ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </RadialGradientBrush.Transform>{settings.NewLine}");
        }

        if (radialGradientBrush.GradientStops.Count > 0)
        {
            sb.Append($"  <RadialGradientBrush.GradientStops>{settings.NewLine}");

            foreach (var stop in radialGradientBrush.GradientStops)
            {
                var color = GeneratorHelper.ToHexColor(stop.Color);
                var offset = GeneratorHelper.ToXamlString(stop.Offset);
                sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{settings.NewLine}");
            }

            sb.Append($"  </RadialGradientBrush.GradientStops>{settings.NewLine}");
        }

        sb.Append($"</RadialGradientBrush>{settings.NewLine}");

        return sb.ToString();
    }

    public static string GenerateTwoPointConicalGradientBrush(TwoPointConicalGradientBrush twoPointConicalGradientBrush, XamlGeneratorSettings settings)
    {
        var sb = new StringBuilder();

        // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
        // ReSharper disable once UnusedVariable
        var startRadius = twoPointConicalGradientBrush.StartRadius;

        // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
        // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
        var endRadius = twoPointConicalGradientBrush.EndRadius;

        var center = twoPointConicalGradientBrush.End;
        var gradientOrigin = twoPointConicalGradientBrush.Start;

        if (!settings.UseCompatMode)
        {
            endRadius = endRadius / twoPointConicalGradientBrush.Bounds.Width;
        }

        sb.Append($"<RadialGradientBrush{GeneratorHelper.ToKey(twoPointConicalGradientBrush.Key)}");

        sb.Append($" Center=\"{GeneratorHelper.ToPoint(center)}\"");
        sb.Append($" GradientOrigin=\"{GeneratorHelper.ToPoint(gradientOrigin)}\"");

        if (settings.UseCompatMode)
        {
            sb.Append($" RadiusX=\"{GeneratorHelper.ToXamlString(endRadius)}\"");
            sb.Append($" RadiusY=\"{GeneratorHelper.ToXamlString(endRadius)}\"");
        }
        else
        {
            sb.Append($" Radius=\"{GeneratorHelper.ToXamlString(endRadius)}\"");
        }

        if (settings.UseCompatMode)
        {
            sb.Append($" MappingMode=\"Absolute\"");
        }

        if (twoPointConicalGradientBrush.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" SpreadMethod=\"{GeneratorHelper.ToGradientSpreadMethod(twoPointConicalGradientBrush.Mode)}\"");
        }

        sb.Append($">{settings.NewLine}");

        if (twoPointConicalGradientBrush.LocalMatrix is { })
        {
            var localMatrix = twoPointConicalGradientBrush.LocalMatrix.Value;

            sb.Append($"  <RadialGradientBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{GeneratorHelper.ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </RadialGradientBrush.Transform>{settings.NewLine}");
        }

        if (twoPointConicalGradientBrush.GradientStops.Count > 0)
        {
            sb.Append($"  <RadialGradientBrush.GradientStops>{settings.NewLine}");

            foreach (var stop in twoPointConicalGradientBrush.GradientStops)
            {
                var color = GeneratorHelper.ToHexColor(stop.Color);
                var offset = GeneratorHelper.ToXamlString(stop.Offset);
                sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{settings.NewLine}");
            }

            sb.Append($"  </RadialGradientBrush.GradientStops>{settings.NewLine}");
        }

        sb.Append($"</RadialGradientBrush>{settings.NewLine}");

        return sb.ToString();
    }

    public static string GeneratePictureBrush(PictureBrush pictureBrush, XamlGeneratorSettings settings, Func<Image, XamlGeneratorSettings, SkiaSharp.SKMatrix?, string> generateImage)
    {
        if (pictureBrush.Picture is null)
        {
            return "";
        }

        var sb = new StringBuilder();

        var sourceRect = pictureBrush.CullRect;
        var destinationRect = pictureBrush.Tile;

        // TODO: Use different visual then Image ?
        sb.Append($"<VisualBrush{GeneratorHelper.ToKey(pictureBrush.Key)}");

        if (pictureBrush.TileMode != ShimSkiaSharp.SKShaderTileMode.Clamp)
        {
            sb.Append($" TileMode=\"{GeneratorHelper.ToTileMode(pictureBrush.TileMode)}\"");
        }

        if (settings.UseCompatMode)
        {
            if (!sourceRect.IsEmpty)
            {
                sb.Append($" Viewport=\"{GeneratorHelper.ToRect(sourceRect)}\" ViewportUnits=\"Absolute\"");
            }

            if (!destinationRect.IsEmpty)
            {
                sb.Append($" Viewbox=\"{GeneratorHelper.ToRect(destinationRect)}\" ViewboxUnits=\"Absolute\"");
            }
        }
        else
        {
            if (!sourceRect.IsEmpty)
            {
                sb.Append($" SourceRect=\"{GeneratorHelper.ToRect(sourceRect)}\"");
            }

            if (!destinationRect.IsEmpty)
            {
                sb.Append($" DestinationRect=\"{GeneratorHelper.ToRect(destinationRect)}\"");
            }
        }

        sb.Append($">{settings.NewLine}");

        if (pictureBrush.LocalMatrix is { })
        {
            var localMatrix = pictureBrush.LocalMatrix.Value;

            sb.Append($"  <VisualBrush.Transform>{settings.NewLine}");
            sb.Append($"    <MatrixTransform Matrix=\"{GeneratorHelper.ToMatrix(localMatrix)}\"/>{settings.NewLine}");
            sb.Append($"  </VisualBrush.Transform>{settings.NewLine}");
        }

        if (pictureBrush.Picture is not null)
        {
            sb.Append($"  <VisualBrush.Visual>{settings.NewLine}");
            sb.Append(generateImage(pictureBrush.Picture, settings, SkiaSharp.SKMatrix.CreateIdentity()));
            sb.Append($"{settings.NewLine}");
            sb.Append($"  </VisualBrush.Visual>{settings.NewLine}");
        }

        sb.Append($"</VisualBrush>{settings.NewLine}");

        return sb.ToString();
    }
}
