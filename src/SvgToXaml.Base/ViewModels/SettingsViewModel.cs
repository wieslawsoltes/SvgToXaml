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
using System.Text.Json.Serialization;

namespace SvgToXaml.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private bool _enableGenerateImage;
    private bool _enableGeneratePreview;
    private bool _useResources;
    private bool _reuseExistingResources;
    private bool _transformGeometry;
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
        set => SetProperty(ref _enableGenerateImage, value);
    }

    [JsonInclude]
    public bool EnableGeneratePreview
    {
        get => _enableGeneratePreview;
        set => SetProperty(ref _enableGeneratePreview, value);
    }

    [JsonInclude]
    public bool UseResources
    {
        get => _useResources;
        set => SetProperty(ref _useResources, value);
    }

    [JsonInclude]
    public bool ReuseExistingResources
    {
        get => _reuseExistingResources;
        set => SetProperty(ref _reuseExistingResources, value);
    }

    [JsonInclude]
    public bool TransformGeometry
    {
        get => _transformGeometry;
        set => SetProperty(ref _transformGeometry, value);
    }

    [JsonInclude]
    public bool UseCompatMode
    {
        get => _useCompatMode;
        set => SetProperty(ref _useCompatMode, value);
    }

    [JsonInclude]
    public bool AddTransparentBackground
    {
        get => _addTransparentBackground;
        set => SetProperty(ref _addTransparentBackground, value);
    }

    [JsonInclude]
    public bool IgnoreOpacity
    {
        get => _ignoreOpacity;
        set => SetProperty(ref _ignoreOpacity, value);
    }

    [JsonInclude]
    public bool IgnoreFilter
    {
        get => _ignoreFilter;
        set => SetProperty(ref _ignoreFilter, value);
    }

    [JsonInclude]
    public bool IgnoreClipPath
    {
        get => _ignoreClipPath;
        set => SetProperty(ref _ignoreClipPath, value);
    }

    [JsonInclude]
    public bool IgnoreMask
    {
        get => _ignoreMask;
        set => SetProperty(ref _ignoreMask, value);
    }

    [JsonConstructor]
    public SettingsViewModel()
    {
    }
}
