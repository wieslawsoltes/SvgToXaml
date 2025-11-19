using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Svg.Skia;
using SvgToXaml.Converter;
using SvgToXaml.Model;

var inputPath = "";
var outputPath = "";
var generatorType = GeneratorType.Drawing;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-g" || args[i] == "--generator")
    {
        if (i + 1 < args.Length)
        {
            if (Enum.TryParse<GeneratorType>(args[i + 1], true, out var result))
            {
                generatorType = result;
                i++;
            }
        }
    }
    else if (string.IsNullOrEmpty(inputPath))
    {
        inputPath = args[i];
    }
    else if (string.IsNullOrEmpty(outputPath))
    {
        outputPath = args[i];
    }
}

if (string.IsNullOrEmpty(inputPath))
{
    Console.WriteLine("Usage: svgxaml <InputPath> [OutputPath] [-g|--generator Drawing|Canvas]");
    return;
}

try
{
    if (File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory))
    {
        var paths = new List<string>();

        GetFiles(inputPath, paths);

        paths.Sort();

        if (paths.Count == 0)
        {
            return;
        }

        var converter = new SvgToXamlConverter()
        {
            UseCompatMode = false,
            ReuseExistingResources = false,
            TransformGeometry = false,
            GeneratorType = generatorType,
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

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(converter.Format(xaml));
            return;
        }

        File.WriteAllText(outputPath, converter.Format(xaml));
    }
    else
    {
        var converter = new SvgToXamlConverter()
        {
            UseCompatMode = false,
            ReuseExistingResources = false,
            TransformGeometry = false,
            GeneratorType = generatorType,
            Resources = null
        };

        var skSvg = new SKSvg();
        skSvg.Load(inputPath);

        var xaml = converter.ToXamlImage(skSvg.Model);

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(converter.Format(xaml));
            return;
        }

        File.WriteAllText(outputPath, converter.Format(xaml));
    }
}
catch (Exception ex)
{
    Console.WriteLine($"{ex.Message}");
    Console.WriteLine($"{ex.StackTrace}");
}

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
