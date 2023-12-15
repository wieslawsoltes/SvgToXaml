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
using System;
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
    public ICommand? RemoveCommand { get; private set; }

    [JsonConstructor]
    public FileItemViewModel(string name, string content)
    {
        _name = name;
        _content = content;
    }
        
    public FileItemViewModel(string name, string content, Func<FileItemViewModel, Task> remove) 
        : this(name, content)
    {
        Initialize(remove);
    }

    public void Initialize(Func<FileItemViewModel, Task> remove)
    {
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
