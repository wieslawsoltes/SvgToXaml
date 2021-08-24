using System.Text.Json.Serialization;
using ReactiveUI;

namespace SvgToXaml.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _enableGenerateImage;
        private bool _enableGeneratePreview;
        private bool _useResources;
        private bool _reuseExistingResources;
        private bool _useCompatMode;
        private bool _useBrushTransform;

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
        public bool UseBrushTransform
        {
            get => _useBrushTransform;
            set => this.RaiseAndSetIfChanged(ref _useBrushTransform, value);
        }

        [JsonConstructor]
        public SettingsViewModel()
        {
        }
    }
}
