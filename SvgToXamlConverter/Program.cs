using System;
using System.IO;
using Svg.Skia;

namespace SvgToXamlConverter
{
    class Program
    {
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
                    return;
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
