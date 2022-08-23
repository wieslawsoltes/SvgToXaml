using Avalonia;
using Avalonia.Web.Blazor;

namespace SvgToXaml.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<SvgToXaml.App>()
            // Uncomment to disable GPU
            // .With(new SkiaOptions { CustomGpuFactory = null })
            .SetupWithSingleViewLifetime();
    }
}
