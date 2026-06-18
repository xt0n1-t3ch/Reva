using Avalonia.Controls;
using Reva.App.ViewModels;

namespace Reva.App.Views;

public partial class MappingsView : UserControl
{
    public MappingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MappingsViewModel viewModel && viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
