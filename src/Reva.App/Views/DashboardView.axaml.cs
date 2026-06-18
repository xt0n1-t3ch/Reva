using Avalonia.Controls;
using Avalonia.Interactivity;
using Reva.App.ViewModels;

namespace Reva.App.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
    }

    private void OnViewLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    private void OnRowSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (grid.SelectedItem is DashboardRow row &&
            DataContext is DashboardViewModel viewModel &&
            viewModel.OpenDocumentCommand.CanExecute(row))
        {
            viewModel.OpenDocumentCommand.Execute(row);
        }

        grid.SelectedItem = null;
    }
}
