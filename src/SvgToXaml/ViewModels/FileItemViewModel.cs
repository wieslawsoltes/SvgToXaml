using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Svg.Model;

namespace SvgToXaml.ViewModels
{
    public class FileItemViewModel : ViewModelBase
    {
        private bool _isLoading;
        private string _name;
        private string _path;
        private SvgViewModel? _svg;
        private SkiaSharp.SKPicture? _picture;

        [JsonInclude]
        public string Name
        {
            get => _name;
            private set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        [JsonInclude]
        public string Path
        {
            get => _path;
            private set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        [JsonIgnore]
        public SvgViewModel? Svg
        {
            get => _svg;
            private set => this.RaiseAndSetIfChanged(ref _svg, value);
        }

        [JsonIgnore]
        public SkiaSharp.SKPicture? Picture
        {
            get => _picture;
            private set => this.RaiseAndSetIfChanged(ref _picture, value);
        }

        [JsonIgnore]
        public ICommand? PreviewCommand { get; private set; }

        [JsonIgnore]
        public ICommand? RemoveCommand { get; private set; }

        [JsonConstructor]
        public FileItemViewModel(string name, string path)
        {
            _name = name;
            _path = path;
        }
        
        public FileItemViewModel(string name, string path, Func<FileItemViewModel, Task> preview, Func<FileItemViewModel, Task> remove) 
            : this(name, path)
        {
            Initialize(preview, remove);
        }

        public void Initialize(Func<FileItemViewModel, Task> preview, Func<FileItemViewModel, Task> remove)
        {
            PreviewCommand = ReactiveCommand.CreateFromTask(async () => await preview(this));

            RemoveCommand = ReactiveCommand.Create(async () => await remove(this)); 
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
                    Picture = Svg.Load(Path, ignoreAttribute);
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
}
