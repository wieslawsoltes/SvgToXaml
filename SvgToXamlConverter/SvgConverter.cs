using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SvgToXamlConverter
{
    public class SvgConverter
    {
        public string NewLine { get; set; } = "\r\n";

        public bool UseCompatMode { get; set; } = false;

        public bool UseBrushTransform { get; set; } = false;

        public string ToXamlDrawingGroup(ShimSkiaSharp.SKPicture? skPicture, ResourceDictionary? resources = null, bool reuseExistingResources = false, string? key = null)
        {
            var drawingGroup = new DrawingGroup(skPicture, resources, key);

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform,
                ReuseExistingResources = reuseExistingResources,
                WriteResources = false,
                Resources = resources
            };

            return drawingGroup.Generate(context);
        }

        public string ToXamlImage(ShimSkiaSharp.SKPicture? skPicture, ResourceDictionary? resources = null, bool reuseExistingResources = false, string? key = null, bool writeResources = true)
        {
            var drawingGroup = new DrawingGroup(skPicture, resources);

            var drawingImage = new DrawingImage(drawingGroup);

            var image = new Image(drawingImage, key);

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform,
                ReuseExistingResources = reuseExistingResources,
                WriteResources = writeResources,
                Resources = resources
            };

            return image.Generate(context);
        }

        public string ToXamlStyles(List<string> paths, ResourceDictionary? resources = null, bool reuseExistingResources = false, bool generateImage = false, bool generatePreview = true)
        {
            var results = new List<(string Path, string Key, string Xaml)>();

            foreach (var path in paths)
            {
                try
                {
                    var svg = new Svg.Skia.SKSvg();
                    svg.Load(path);
                    if (svg.Model is null)
                    {
                        continue;
                    }

                    var key = $"_{CreateKey(path)}";
                    if (generateImage)
                    {
                        var xaml = ToXamlImage(svg.Model, resources, reuseExistingResources, key, writeResources: false);
                        results.Add((path, key, xaml));
                    }
                    else
                    {
                        var xaml = ToXamlDrawingGroup(svg.Model, resources, reuseExistingResources, key);
                        results.Add((path, key, xaml));
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var sb = new StringBuilder();

            if (UseCompatMode)
            {
                sb.Append($"<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"{NewLine}");
                sb.Append($"                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{NewLine}");
            }
            else
            {
                sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{NewLine}");
                sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{NewLine}");
            }

            if (generatePreview && !UseCompatMode)
            {
                sb.Append($"  <Design.PreviewWith>");
                sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">");
                sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">");

                foreach (var result in results)
                {
                    if (generateImage)
                    {
                        sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>");
                    }
                    else
                    {
                        sb.Append($"        <Image>");

                        if (UseCompatMode)
                        {
                            sb.Append($"            <Image.Source>");
                        }

                        sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>");

                        if (UseCompatMode)
                        {
                            sb.Append($"            </Image.Source>");
                        }

                        sb.Append($"        </Image>");
                    }
                }

                sb.Append($"      </WrapPanel>");
                sb.Append($"    </ScrollViewer>");
                sb.Append($"  </Design.PreviewWith>");
            }

            if (!UseCompatMode)
            {
                sb.Append($"  <Style>{NewLine}");
                sb.Append($"    <Style.Resources>{NewLine}");
            }

            if (resources is { } && (resources.Brushes.Count > 0 || resources.Pens.Count > 0))
            {
                var context = new GeneratorContext
                {
                    NewLine = NewLine,
                    UseCompatMode = UseCompatMode,
                    UseBrushTransform = UseBrushTransform,
                    ReuseExistingResources = reuseExistingResources,
                    WriteResources = false,
                    Resources = resources
                };

                sb.Append(resources.Generate(context));
            }

            foreach (var result in results)
            {
                sb.Append($"<!-- {Path.GetFileName(result.Path)} -->{NewLine}");
                sb.Append(result.Xaml);
                sb.Append(NewLine);
            }

            if (UseCompatMode)
            {
                sb.Append($"</ResourceDictionary>");
            }
            else
            {
                sb.Append($"    </Style.Resources>{NewLine}");
                sb.Append($"  </Style>{NewLine}");
                sb.Append($"</Styles>");
            }

            return sb.ToString();
        }

        public virtual string CreateKey(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string key = name.Replace("-", "_");
            return $"_{key}";
        }

        public string Format(string xml)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append($"<Root");
                sb.Append(UseCompatMode
                    ? $" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\""
                    : $" xmlns=\"https://github.com/avaloniaui\"");
                sb.Append($" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");
                sb.Append($">");
                sb.Append(xml);
                sb.Append($"</Root>");

                using var ms = new MemoryStream();
                using var writer = new XmlTextWriter(ms, Encoding.UTF8);
                var document = new XmlDocument();
                document.LoadXml(sb.ToString());
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                document.WriteContentTo(writer);
                writer.Flush();
                ms.Flush();
                ms.Position = 0;
                using var sReader = new StreamReader(ms);
                var formatted = sReader.ReadToEnd();

                var lines = formatted.Split(NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var inner = lines.Skip(1).Take(lines.Length - 2).Select(x => x.Substring(2, x.Length - 2));
                return string.Join(NewLine, inner);
            }
            catch
            {
                // ignored
            }

            return "";
        }
    }
}
