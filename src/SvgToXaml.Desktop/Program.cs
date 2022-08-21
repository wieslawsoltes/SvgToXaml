using System;
using Avalonia;
using Avalonia.Controls.Skia;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;

namespace SvgToXaml;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(SKPictureControl).Assembly);
        GC.KeepAlive(typeof(Behavior).Assembly);
        GC.KeepAlive(typeof(ComparisonConditionType).Assembly);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseSkia()
            .With(new Win32PlatformOptions()
            {
            })
            .With(new X11PlatformOptions()
            {
            })
            .With(new AvaloniaNativePlatformOptions()
            {
            });
    }
}
