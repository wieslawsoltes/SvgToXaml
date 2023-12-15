/*
 * SvgToXaml A Svg to Xaml converter.
 * Copyright (C) 2023  Wiesław Šoltés
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
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
            TransformGeometry = false,
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
