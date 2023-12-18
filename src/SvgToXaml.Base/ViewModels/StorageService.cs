using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace SvgToXaml.ViewModels;

internal static class StorageService
{
    public static FilePickerFileType All { get; } = new("All")
    {
        Patterns = new[] { "*.*" },
        MimeTypes = new[] { "*/*" }
    };

    public static FilePickerFileType Json { get; } = new("Json")
    {
        Patterns = new[] { "*.json" },
        AppleUniformTypeIdentifiers = new[] { "public.json" },
        MimeTypes = new[] { "application/json" }
    };

    public static FilePickerFileType ImageSvg { get; } = new("Svg")
    {
        Patterns = new[] { "*.svg" },
        AppleUniformTypeIdentifiers = new[] { "public.svg-image" },
        MimeTypes = new[] { "image/svg+xml" }
    };

    public static FilePickerFileType ImageSvgz { get; } = new("Svgz")
    {
        Patterns = new[] { "*.svgz" },
        // TODO:
        AppleUniformTypeIdentifiers = new[] { "public.svg-image" },
        // TODO:
        MimeTypes = new[] { "image/svg+xml" }
    };

    public static FilePickerFileType Xaml { get; } = new("Xaml")
    {
        Patterns = new[] { "*.xaml" },
        // TODO:
        AppleUniformTypeIdentifiers = new[] { "public.xaml" },
        // TODO:
        MimeTypes = new[] { "application/xaml" }
    };

    public static FilePickerFileType Axaml { get; } = new("Axaml")
    {
        Patterns = new[] { "*.axaml" },
        // TODO:
        AppleUniformTypeIdentifiers = new[] { "public.axaml" },
        // TODO:
        MimeTypes = new[] { "application/axaml" }
    };

    public static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window.StorageProvider;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime { MainView: { } mainView })
        {
            var visualRoot = mainView.GetVisualRoot();
            if (visualRoot is TopLevel topLevel)
            {
                return topLevel.StorageProvider;
            }
        }

        return null;
    }
}
