using System;
using Avalonia;
using Avalonia.Controls.Skia;
using Avalonia.ReactiveUI;
using Avalonia.Xaml.Interactions.Core;
using Avalonia.Xaml.Interactivity;

namespace SvgToXaml
{
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
                //.UseDirect2D1()
                .With(new Win32PlatformOptions()
                {
                    UseDeferredRendering = true,
                    AllowEglInitialization = true,
                    UseWindowsUIComposition = true
                })
                .With(new X11PlatformOptions()
                {
                    UseDeferredRendering = true
                })
                .With(new AvaloniaNativePlatformOptions()
                {
                    UseDeferredRendering = true
                })
                .UseReactiveUI();
        }
    }
}
