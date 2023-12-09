using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Svg.Model;

namespace SvgToXaml.ViewModels;

public class ProjectViewModel : ViewModelBase
{
    private FileItemViewModel? _selectedItem;
    private ObservableCollection<FileItemViewModel> _items;
    private SettingsViewModel _settings;

    [JsonIgnore]
    public FileItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    [JsonInclude]
    public ObservableCollection<FileItemViewModel> Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    [JsonInclude]
    public SettingsViewModel Settings
    {
        get => _settings;
        set => SetProperty(ref _settings, value);
    }

    [JsonConstructor]
    public ProjectViewModel()
    {
        _items = new ObservableCollection<FileItemViewModel>();

        _settings = new SettingsViewModel()
        {
            EnableGenerateImage = true,
            EnableGeneratePreview = true,
            UseResources = true,
            ReuseExistingResources = true,
            TransformGeometry = true,
            UseCompatMode = false,
            AddTransparentBackground = true,
            IgnoreOpacity = false,
            IgnoreFilter = false,
            IgnoreClipPath = false,
            IgnoreMask = false,
        };
    }

    public DrawAttributes GetIgnoreAttributes()
    {
        var ignoreAttribute = DrawAttributes.None;

        if (_settings.IgnoreOpacity)
        {
            ignoreAttribute |= DrawAttributes.Opacity;
        }

        if (_settings.IgnoreFilter)
        {
            ignoreAttribute |= DrawAttributes.Filter;
        }

        if (_settings.IgnoreClipPath)
        {
            ignoreAttribute |= DrawAttributes.ClipPath;
        }

        if (_settings.IgnoreMask)
        {
            ignoreAttribute |= DrawAttributes.Mask;
        }

        return ignoreAttribute;
    }
}
