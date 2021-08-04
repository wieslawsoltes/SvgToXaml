using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SvgToXaml.ViewModels;

namespace SvgToXaml.Views
{
    public class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Panel_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Panel panel && panel.DataContext is FileItemViewModel fileItemViewModel)
            {
                if (fileItemViewModel.PreviewCommand.CanExecute(Unit.Default))
                {
                    fileItemViewModel.PreviewCommand.Execute(Unit.Default);
                }
            }
        }
    }
}

