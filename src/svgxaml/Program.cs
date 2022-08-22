using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Svg.Skia;
using SvgToXamlConverter.Model;

namespace svgxaml;

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

    static Stream LoadFromStream(Stream stream, string name)
    {
        var extension = Path.GetExtension(name);
        var memoryStream = new MemoryStream();
        
        if (extension == "svgz")
        { 
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress); 
            gzipStream.CopyTo(memoryStream);
        }
        else
        {
            stream.CopyTo(memoryStream);
        }

        memoryStream.Position = 0;
        return memoryStream;
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

                var converter = new SvgToXamlConverter.SvgToXamlConverter()
                {
                    UseCompatMode = false,
                    ReuseExistingResources = false,
                    Resources = null
                };

                var xaml = converter.ToXamlStyles(paths.Select(x =>
                {
                    using var stream = File.OpenRead(x);
                    var ms = LoadFromStream(stream, x);
                    using var reader = new StreamReader(ms);
                    var content = reader.ReadToEnd();
                    return new InputItem(Path.GetFileName(x), content);
                }).ToList());

                if (args.Length == 1)
                {
                    Console.WriteLine(converter.Format(xaml));
                    return;
                }

                if (args.Length == 2)
                {
                    var outputPath = args[1];
                    File.WriteAllText(outputPath, converter.Format(xaml));
                }
            }
            else
            {
                var converter = new SvgToXamlConverter.SvgToXamlConverter()
                {
                    UseCompatMode = false,
                    ReuseExistingResources = false,
                    Resources = null
                };

                var skSvg = new SKSvg();
                skSvg.Load(inputPath);

                var xaml = converter.ToXamlImage(skSvg.Model);

                if (args.Length == 1)
                {
                    Console.WriteLine(converter.Format(xaml));
                    return;
                }

                if (args.Length == 2)
                {
                    var outputPath = args[1];
                    File.WriteAllText(outputPath, converter.Format(xaml));
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
