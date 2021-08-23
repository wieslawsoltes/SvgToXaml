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

        public bool UseCompatMode { get; set; }

        public bool UseBrushTransform { get; set; }

        public bool ReuseExistingResources { get; set; }

        public ResourceDictionary? Resources { get; set; }

        public string ToXamlDrawingGroup(ShimSkiaSharp.SKPicture? skPicture, string? key = null)
        {
            var drawingGroup = new DrawingGroup(skPicture, Resources, key);

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform,
                ReuseExistingResources = ReuseExistingResources,
                WriteResources = false,
                Resources = Resources
            };

            return drawingGroup.Generate(context);
        }

        public string ToXamlImage(ShimSkiaSharp.SKPicture? skPicture, string? key = null, bool writeResources = true)
        {
            var drawingGroup = new DrawingGroup(skPicture, Resources);
            var drawingImage = new DrawingImage(drawingGroup);
            var image = new Image(drawingImage, key);

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform,
                ReuseExistingResources = ReuseExistingResources,
                WriteResources = writeResources,
                Resources = Resources
            };

            return image.Generate(context);
        }

        public string ToXamlStyles(List<string> paths, bool generateImage = false, bool generatePreview = true)
        {
            var results = new List<(string Path, string Key, Resource Resource)>();
 
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
                        var drawingGroup = new DrawingGroup(svg.Model, Resources);
                        var drawingImage = new DrawingImage(drawingGroup);
                        var image = new Image(drawingImage, key);
                        results.Add((path, key, image));
                    }
                    else
                    {
                        var drawingGroup = new DrawingGroup(svg.Model, Resources, key);
                        results.Add((path, key, drawingGroup));
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var resources = results.Select(x => x.Resource).ToList();
            var styles = new Styles(resources, generateImage, generatePreview);

            var context = new GeneratorContext
            {
                NewLine = NewLine,
                UseCompatMode = UseCompatMode,
                UseBrushTransform = UseBrushTransform,
                ReuseExistingResources = ReuseExistingResources,
                WriteResources = false,
                Resources = Resources
            };

            return styles.Generate(context);
        }

        public virtual string CreateKey(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string key = name.Replace("-", "_");
            return $"_{key}";
        }

        public virtual string Format(string xml)
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
