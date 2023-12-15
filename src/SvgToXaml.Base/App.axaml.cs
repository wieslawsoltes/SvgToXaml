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
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ThemeManager;
using SvgToXaml.ViewModels;
using SvgToXaml.Views;

namespace SvgToXaml;

public class App : Application
{
    public static IThemeManager? ThemeManager;

    private const string ProjectFileName = "project.json";

    public override void Initialize()
    {
#if true
        ThemeManager = new FluentThemeManager();
#else
        ThemeManager = new SimpleThemeManager();
#endif
        ThemeManager.Initialize(this);
        ThemeManager.Switch(1);

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = new MainWindowViewModel();

            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            desktop.MainWindow = mainWindow;

            desktop.Startup += (_, _) =>
            {
                if (File.Exists(ProjectFileName))
                {
                    var json = File.ReadAllText(ProjectFileName);
                    var project = JsonSerializer.Deserialize<ProjectViewModel>(json);
                    if (project is { })
                    {
                        mainViewModel.Project = project;

                        foreach (var fileItemViewModel in mainViewModel.Project.Items)
                        {
                            mainViewModel.Initialize(fileItemViewModel);
                        }
                    }
                }
            };

            desktop.Exit += (_, _) =>
            {
                var json = JsonSerializer.Serialize(mainViewModel.Project);
                File.WriteAllText(ProjectFileName, json);
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            var mainViewModel = new MainWindowViewModel();

            var mainView = new MainView()
            {
                DataContext = mainViewModel
            };

            single.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
