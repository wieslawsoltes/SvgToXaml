using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Svg.Skia;

namespace SvgToXamlConverter
{
    class Program
    {
        static void GetFiles(string path, List<string> paths)
        {
            if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                var svgPaths = Directory.EnumerateFiles(path, "*.svg", new EnumerationOptions {RecurseSubdirectories = true});
                var svgzPaths = Directory.EnumerateFiles(path, "*.svgz", new EnumerationOptions {RecurseSubdirectories = true});
                svgPaths.ToList().ForEach(x => GetFiles(x, paths));
                svgzPaths.ToList().ForEach(x => GetFiles(x, paths));
                return;
            }

            var extension = Path.GetExtension(path);
            switch (extension.ToLower())
            {
                case ".svg":
                case ".svgz":
                    paths.Add(path);
                    break;
            }
        }

        static void Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("Usage: SvgToXamlConverter <InputPath> [OutputPath]");
                return;
            }

            try
            {
                var inputPath = args[0];

                if (File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory))
                {
                    var paths = new List<string>();

                    GetFiles(inputPath, paths);

                    if (paths.Count == 0)
                    {
                        return;
                    }

                    var generateImage = false;
                    var generateStyles = true;
                    var indent = generateStyles ? "      " : "";
                    var xaml = default(string);

                    if (generateStyles)
                    {
                        xaml += $"<Styles xmlns=\"https://github.com/avaloniaui\"{SvgConverter.NewLine}";
                        xaml += $"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{SvgConverter.NewLine}";
                        xaml += $"  <Style>{SvgConverter.NewLine}";
                        xaml += $"    <Style.Resources>{SvgConverter.NewLine}";
                    }

                    for (var i = 0; i < paths.Count; i++)
                    {
                        var path = paths[i];
                        var svg = new SKSvg();
                        var picture = svg.Load(path);
                        xaml += $"{indent}<!-- {path} -->{SvgConverter.NewLine}";
                        xaml += SvgConverter.ToXaml(svg.Model, generateImage: generateImage, indent: indent, key: generateStyles ? $"_{i.ToString()}" : null);
                        xaml += SvgConverter.NewLine;
                    }

                    if (generateStyles)
                    {
                        xaml += $"    </Style.Resources>{SvgConverter.NewLine}";
                        xaml += $"  </Style>{SvgConverter.NewLine}";
                        xaml += $"</Styles>{SvgConverter.NewLine}";
                    }

                    if (args.Length == 1)
                    {
                        Console.WriteLine(xaml);
                        return;
                    }

                    if (args.Length == 2)
                    {
                        var outputPath = args[1];
                        File.WriteAllText(outputPath, xaml);
                    }
                }
                else
                {
                    var svg = new SKSvg();
                    var picture = svg.Load(inputPath);
                    var xaml = SvgConverter.ToXaml(svg.Model);

                    if (args.Length == 1)
                    {
                        Console.WriteLine(xaml);
                        return;
                    }

                    if (args.Length == 2)
                    {
                        var outputPath = args[1];
                        File.WriteAllText(outputPath, xaml);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                Console.WriteLine($"{ex.StackTrace}");
            }
        }
    }
}
