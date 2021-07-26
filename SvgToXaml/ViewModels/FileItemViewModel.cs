using System;
using System.Windows.Input;
using ReactiveUI;
using SkiaSharp;
using Svg.Skia;

namespace SvgToXaml.ViewModels
{
    public class FileItemViewModel : ViewModelBase
    {
        private string _name;
        private string _path;
        private SKSvg? _svg;
        private SKPicture? _picture;

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

        public SKPicture? Picture
        {
            get => _picture;
            private set => this.RaiseAndSetIfChanged(ref _picture, value);
        }

        public ICommand RemoveCommand { get; }

        public FileItemViewModel(string name, string path, Action<FileItemViewModel> remove)
        {
            _name = name;
            _path = path;
            _svg = new SKSvg();
            _picture = _svg.Load(_path);

            RemoveCommand = ReactiveCommand.Create(() => remove(this));
        }
    }
}