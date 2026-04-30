using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SvgToXaml.ViewModels;
using SvgToXaml.Views;

namespace SvgToXaml;

public class App : Application
{
    private const string ProjectFileName = "project.json";

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
