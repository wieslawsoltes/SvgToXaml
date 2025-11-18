using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Svg.Skia;
using SvgToXaml.Converter;
using SvgToXaml.Model;
using Svg.Model;

namespace SvgToXaml.Build;

public class SvgToXamlTask : Task
{
    [Required]
    public ITaskItem[] InputFiles { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            if (InputFiles.Length == 0)
            {
                return true;
            }

            // Group by OutputFile
            var fileGroups = InputFiles
                .Where(f => !string.IsNullOrEmpty(f.GetMetadata("OutputFile")))
                .GroupBy(f => f.GetMetadata("OutputFile"));

            foreach (var group in fileGroups)
            {
                var outputFile = group.Key;
                var firstItem = group.First();
                var converter = CreateConverter(firstItem);
                var inputItems = new List<InputItem>();

                foreach (var item in group)
                {
                    var path = item.ItemSpec;
                    if (File.Exists(path))
                    {
                        var content = File.ReadAllText(path);
                        inputItems.Add(new InputItem(Path.GetFileName(path), content));
                    }
                }

                var xaml = converter.ToXamlStyles(inputItems);
                var formatted = converter.Format(xaml);
                
                var dir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                File.WriteAllText(outputFile, formatted);
                Log.LogMessage(MessageImportance.High, $"Generated {outputFile}");
            }

            // Handle individual files (OutputDirectory set, OutputFile not set)
            var individualFiles = InputFiles
                .Where(f => string.IsNullOrEmpty(f.GetMetadata("OutputFile")) && !string.IsNullOrEmpty(f.GetMetadata("OutputDirectory")));

            foreach (var item in individualFiles)
            {
                var outputDirectory = item.GetMetadata("OutputDirectory");
                var path = item.ItemSpec;
                if (File.Exists(path))
                {
                    var converter = CreateConverter(item);
                    var content = File.ReadAllText(path);
                    var svgDocument = SvgExtensions.FromSvg(content);
                    if (svgDocument != null)
                    {
                        var assetLoader = new SkiaAssetLoader(new SkiaModel(new SKSvgSettings()));
                        var model = SvgExtensions.ToModel(svgDocument, assetLoader, out _, out _, converter.GetDrawAttributes());
                        if (model != null)
                        {
                            var xaml = converter.ToXamlImage(model);
                            var formatted = converter.Format(xaml);
                            
                            if (!Directory.Exists(outputDirectory))
                            {
                                Directory.CreateDirectory(outputDirectory);
                            }

                            var extension = item.GetMetadata("OutputExtension");
                            if (string.IsNullOrEmpty(extension))
                            {
                                extension = ".axaml";
                            }

                            var fileName = Path.ChangeExtension(Path.GetFileName(path), extension);
                            var outputPath = Path.Combine(outputDirectory, fileName);
                            
                            File.WriteAllText(outputPath, formatted);
                            Log.LogMessage(MessageImportance.High, $"Generated {outputPath}");
                        }
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }

    private SvgToXamlConverter CreateConverter(ITaskItem item)
    {
        var newLine = item.GetMetadata("NewLine");
        if (string.IsNullOrEmpty(newLine))
        {
            newLine = "\r\n";
        }

        var useResources = GetBool(item, "UseResources");

        return new SvgToXamlConverter()
        {
            NewLine = newLine,
            UseCompatMode = GetBool(item, "UseCompatMode"),
            AddTransparentBackground = GetBool(item, "AddTransparentBackground"),
            ReuseExistingResources = GetBool(item, "ReuseExistingResources"),
            TransformGeometry = GetBool(item, "TransformGeometry"),
            IgnoreOpacity = GetBool(item, "IgnoreOpacity"),
            IgnoreFilter = GetBool(item, "IgnoreFilter"),
            IgnoreClipPath = GetBool(item, "IgnoreClipPath"),
            IgnoreMask = GetBool(item, "IgnoreMask"),
            Resources = useResources ? new SvgToXaml.Model.Resources.ResourceDictionary() : null
        };
    }

    private bool GetBool(ITaskItem item, string name)
    {
        var value = item.GetMetadata(name);
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        return false;
    }
}
