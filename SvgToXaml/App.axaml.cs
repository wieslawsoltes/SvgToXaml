using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SvgToXaml.ViewModels;
using SvgToXaml.Views;

namespace SvgToXaml
{
    public class App : Application
    {
        private const string SettingsFileName = "settings.json";

        private const string PathsFileName = "paths.txt";

        public override void Initialize()
        {
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
                    if (File.Exists(SettingsFileName))
                    {
                        var json = File.ReadAllText(SettingsFileName);
                        var settings = JsonSerializer.Deserialize<SettingsViewModel>(json);
                        if (settings is { })
                        {
                            mainViewModel.Settings = settings;
                        }
                    }
                    
                    if (File.Exists(PathsFileName))
                    {
                        var paths = File.ReadAllLines(PathsFileName);
                        if (paths.Length > 0)
                        {
                            mainViewModel.Add(paths);
                        }
                    }
                };

                desktop.Exit += (_, _) =>
                {
                    var paths = mainViewModel.Items?.Select(x => x.Path);
                    if (paths is { })
                    {
                        File.WriteAllLines(PathsFileName, paths);
                    }

                    var json = JsonSerializer.Serialize(mainViewModel.Settings);
                    File.WriteAllText(SettingsFileName, json);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
