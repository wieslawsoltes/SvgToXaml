using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Svg.Model;

namespace SvgToXaml.ViewModels;

public class FileItemViewModel : ViewModelBase
{
    private bool _isLoading;
    private string _name;
    private string _content;
    private SvgViewModel? _svg;
    private SkiaSharp.SKPicture? _picture;

    [JsonInclude]
    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    [JsonInclude]
    public string Content
    {
        get => _content;
        private set => SetProperty(ref _content, value);
    }

    [JsonIgnore]
    public SvgViewModel? Svg
    {
        get => _svg;
        private set => SetProperty(ref _svg, value);
    }

    [JsonIgnore]
    public SkiaSharp.SKPicture? Picture
    {
        get => _picture;
        private set => SetProperty(ref _picture, value);
    }

    [JsonIgnore]
    public ICommand? PreviewCommand { get; private set; }

    [JsonIgnore]
    public ICommand? RemoveCommand { get; private set; }

    [JsonConstructor]
    public FileItemViewModel(string name, string content)
    {
        _name = name;
        _content = content;
    }
        
    public FileItemViewModel(string name, string content, Func<FileItemViewModel, Task> preview, Func<FileItemViewModel, Task> remove) 
        : this(name, content)
    {
        Initialize(preview, remove);
    }

    public void Initialize(Func<FileItemViewModel, Task> preview, Func<FileItemViewModel, Task> remove)
    {
        PreviewCommand = new AsyncRelayCommand(async () => await preview(this));

        RemoveCommand = new AsyncRelayCommand(async () => await remove(this)); 
    }
 
    public async Task Load(DrawAttributes ignoreAttribute)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        if (Picture is null)
        {
            await Task.Run(() =>
            {
                Svg = new SvgViewModel();
                Picture = Svg.FromSvg(Content, ignoreAttribute);
            });
        }

        _isLoading = false;
    }

    public void Clean()
    {
        if (Picture is not null)
        {
            Picture?.Dispose();
            Svg?.Dispose();
            Picture = null;
            Svg = null;
        }
    }
}
