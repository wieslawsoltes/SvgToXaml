/*
 * SvgToXaml A Svg to Xaml converter.
 * Copyright (C) 2023  Wiesław Šoltés
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Svg.Skia;
using SvgToXaml.Converter;
using SvgToXaml.Model;

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

        var converter = new SvgToXamlConverter()
        {
            UseCompatMode = false,
            ReuseExistingResources = false,
            TransformGeometry = false,
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
        var converter = new SvgToXamlConverter()
        {
            UseCompatMode = false,
            ReuseExistingResources = false,
            TransformGeometry = false,
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
