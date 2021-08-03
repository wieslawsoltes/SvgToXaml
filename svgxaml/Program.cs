using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Svg.Skia;
using SvgToXamlConverter;

namespace svgxaml
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
                Console.WriteLine("Usage: svgxaml <InputPath> [OutputPath]");
                return;
            }

            try
            {
                var inputPath = args[0];

                if (File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory))
                {
                    var paths = new List<string>();

                    GetFiles(inputPath, paths);

                    paths.Sort();

                    if (paths.Count == 0)
                    {
                        return;
                    }

                    var xaml = SvgConverter.ToXamlStyles(paths, generateImage: false);

                    if (args.Length == 1)
                    {
                        Console.WriteLine(SvgConverter.Format(xaml));
                        return;
                    }

                    if (args.Length == 2)
                    {
                        var outputPath = args[1];
                        File.WriteAllText(outputPath, SvgConverter.Format(xaml));
                    }
                }
                else
                {
                    var svg = new SKSvg();
                    svg.Load(inputPath);
                    var xaml = SvgConverter.ToXamlImage(svg.Model, key: null);

                    if (args.Length == 1)
                    {
                        Console.WriteLine(SvgConverter.Format(xaml));
                        return;
                    }

                    if (args.Length == 2)
                    {
                        var outputPath = args[1];
                        File.WriteAllText(outputPath, SvgConverter.Format(xaml));
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
