using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Svg.Skia;

namespace SvgToXaml.ViewModels
{
    public class FileItemViewModel : ViewModelBase
    {
        private bool _isLoading;
        private string _name;
        private string _path;
        private SKSvg? _svg;
        private SkiaSharp.SKPicture? _picture;

        public string Name
        {
            get => _name;
            private set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Path
        {
            get => _path;
            private set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        public SKSvg? Svg
        {
            get => _svg;
            private set => this.RaiseAndSetIfChanged(ref _svg, value);
        }

        public SkiaSharp.SKPicture? Picture
        {
            get => _picture;
            private set => this.RaiseAndSetIfChanged(ref _picture, value);
        }

        public ICommand RemoveCommand { get; }

        public FileItemViewModel(string name, string path, Action<FileItemViewModel> remove)
        {
            _name = name;
            _path = path;

            RemoveCommand = ReactiveCommand.Create(() => remove(this));
        }

        public async Task Load()
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
                    Svg = new SKSvg();
                    Picture = Svg.Load(Path);
                });
            }

            _isLoading = false;
        }
    }
}
