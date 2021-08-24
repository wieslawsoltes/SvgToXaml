using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace SvgToXaml.ViewModels
{
    public class ProjectViewModel : ViewModelBase
    {
        private FileItemViewModel? _selectedItem;
        private ObservableCollection<FileItemViewModel> _items;
        private SettingsViewModel _settings;

        [JsonIgnore]
        public FileItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        [JsonInclude]
        public ObservableCollection<FileItemViewModel> Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        [JsonInclude]
        public SettingsViewModel Settings
        {
            get => _settings;
            set => this.RaiseAndSetIfChanged(ref _settings, value);
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
                UseCompatMode = false,
                UseBrushTransform = false,
            };
        }
    }
}
