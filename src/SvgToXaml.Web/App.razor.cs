using Avalonia.Web.Blazor;

namespace SvgToXaml.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<SvgToXaml.App>()
            .SetupWithSingleViewLifetime();
    }
}
