# SvgToXaml

[![Build Status](https://dev.azure.com/wieslawsoltes/GitHub/_apis/build/status/wieslawsoltes.SvgToXaml?branchName=main)](https://dev.azure.com/wieslawsoltes/GitHub/_build/latest?definitionId=92&branchName=main)
[![CI](https://github.com/wieslawsoltes/SvgToXaml/actions/workflows/build.yml/badge.svg)](https://github.com/wieslawsoltes/SvgToXaml/actions/workflows/build.yml)

[![NuGet](https://img.shields.io/nuget/v/SvgToXamlConverter.svg)](https://www.nuget.org/packages/SvgToXamlConverter)
[![NuGet](https://img.shields.io/nuget/dt/SvgToXamlConverter.svg)](https://www.nuget.org/packages/SvgToXamlConverter)

[![GitHub release](https://img.shields.io/github/release/wieslawsoltes/SvgToXaml.svg)](https://github.com/wieslawsoltes/SvgToXaml)
[![Github All Releases](https://img.shields.io/github/downloads/wieslawsoltes/SvgToXaml/total.svg)](https://github.com/wieslawsoltes/SvgToXaml)
[![Github Releases](https://img.shields.io/github/downloads/wieslawsoltes/SvgToXaml/latest/total.svg)](https://github.com/wieslawsoltes/SvgToXaml)

Svg to xaml conveter.

![image](https://user-images.githubusercontent.com/2297442/130685251-185cc489-8724-408b-8965-955f9bc77177.png)

## MSBuild Task

You can use the `SvgToXaml.Build` package to convert SVG files to XAML during the build process.

Add the package reference to your project:

```xml
<PackageReference Include="SvgToXaml.Build" Version="..." />
```

Configure the task in your `.csproj` file by adding `SvgFiles` items. You can set properties as attributes on the items.

### Examples

**Combine all SVGs into one ResourceDictionary:**

```xml
<ItemGroup>
  <SvgFiles Include="Assets\**\*.svg" OutputFile="Assets\Icons.axaml" />
</ItemGroup>
```

**Convert to individual files in a directory:**

```xml
<ItemGroup>
  <SvgFiles Include="Assets\**\*.svg" OutputDirectory="Assets\Converted" />
</ItemGroup>
```

**With settings:**

```xml
<ItemGroup>
  <SvgFiles Include="Assets\**\*.svg" OutputFile="Assets\Icons.axaml" UseCompatMode="true" AddTransparentBackground="true" />
</ItemGroup>
```

### Settings Reference

| Attribute | Description | Default |
| --- | --- | --- |
| `OutputFile` | Path to the output file (ResourceDictionary). If set, all SVGs with the same OutputFile are combined into this file. | - |
| `OutputDirectory` | Path to the output directory. If set (and OutputFile is not), SVGs are converted to individual files in this directory. | - |
| `OutputExtension` | Output file extension when using OutputDirectory. | `.axaml` |
| `UseCompatMode` | Enable compatibility mode. | `false` |
| `AddTransparentBackground` | Add a transparent background to the generated XAML. | `false` |
| `UseResources` | Use resources in the generated XAML. | `false` |
| `ReuseExistingResources` | Reuse existing resources if available. | `false` |
| `TransformGeometry` | Transform geometry in the generated XAML. | `false` |
| `IgnoreOpacity` | Ignore opacity attributes in SVG. | `false` |
| `IgnoreFilter` | Ignore filter attributes in SVG. | `false` |
| `IgnoreClipPath` | Ignore clip-path attributes in SVG. | `false` |
| `IgnoreMask` | Ignore mask attributes in SVG. | `false` |
| `NewLine` | New line character(s) to use in generated files. | `\r\n` |

## License

SvgToXaml is licensed under the [MIT license](LICENSE).
