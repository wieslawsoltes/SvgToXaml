using System.Text.Json.Serialization;
using ReactiveUI;

namespace SvgToXaml.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _enableGenerateImage;
    private bool _enableGeneratePreview;
    private bool _useResources;
    private bool _reuseExistingResources;
    private bool _useCompatMode;
    private bool _addTransparentBackground;
    private bool _ignoreOpacity;
    private bool _ignoreFilter;
    private bool _ignoreClipPath;
    private bool _ignoreMask;

    [JsonInclude]
    public bool EnableGenerateImage
    {
        get => _enableGenerateImage;
        set => this.RaiseAndSetIfChanged(ref _enableGenerateImage, value);
    }

    [JsonInclude]
    public bool EnableGeneratePreview
    {
        get => _enableGeneratePreview;
        set => this.RaiseAndSetIfChanged(ref _enableGeneratePreview, value);
    }

    [JsonInclude]
    public bool UseResources
    {
        get => _useResources;
        set => this.RaiseAndSetIfChanged(ref _useResources, value);
    }

    [JsonInclude]
    public bool ReuseExistingResources
    {
        get => _reuseExistingResources;
        set => this.RaiseAndSetIfChanged(ref _reuseExistingResources, value);
    }

    [JsonInclude]
    public bool UseCompatMode
    {
        get => _useCompatMode;
        set => this.RaiseAndSetIfChanged(ref _useCompatMode, value);
    }

    [JsonInclude]
    public bool AddTransparentBackground
    {
        get => _addTransparentBackground;
        set => this.RaiseAndSetIfChanged(ref _addTransparentBackground, value);
    }

    [JsonInclude]
    public bool IgnoreOpacity
    {
        get => _ignoreOpacity;
        set => this.RaiseAndSetIfChanged(ref _ignoreOpacity, value);
    }

    [JsonInclude]
    public bool IgnoreFilter
    {
        get => _ignoreFilter;
        set => this.RaiseAndSetIfChanged(ref _ignoreFilter, value);
    }

    [JsonInclude]
    public bool IgnoreClipPath
    {
        get => _ignoreClipPath;
        set => this.RaiseAndSetIfChanged(ref _ignoreClipPath, value);
    }

    [JsonInclude]
    public bool IgnoreMask
    {
        get => _ignoreMask;
        set => this.RaiseAndSetIfChanged(ref _ignoreMask, value);
    }

    [JsonConstructor]
    public SettingsViewModel()
    {
    }
}
