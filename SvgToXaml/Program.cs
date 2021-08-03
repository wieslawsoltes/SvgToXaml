using System;
using Avalonia;
using Avalonia.Controls.Skia;
using Avalonia.ReactiveUI;

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

            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseSkia()
                .With(new Win32PlatformOptions()
                {
                    UseDeferredRendering = true
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
