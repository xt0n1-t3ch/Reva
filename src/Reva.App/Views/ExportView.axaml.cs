using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Reva.App.ViewModels;

namespace Reva.App.Views;

public partial class ExportView : UserControl
{
    public ExportView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not ExportViewModel viewModel)
        {
            return;
        }

        viewModel.OpenFileAsync = OpenFileAsync;
        viewModel.SaveFileAsync = SaveFileAsync;

        if (viewModel.LoadCommand.CanExecute(null))
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    private async Task<OpenFileResult?> OpenFileAsync(OpenFileRequest request, CancellationToken cancellationToken)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = request.Title,
            AllowMultiple = false,
            FileTypeFilter = ToFileTypes(request.Filters)
        });

        var file = files.Count == 0 ? null : files[0];
        if (file is null)
        {
            return null;
        }

        var stream = await file.OpenReadAsync();
        return new OpenFileResult(file.Name, stream);
    }

    private async Task<Stream?> SaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken)
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null)
        {
            return null;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = request.Title,
            SuggestedFileName = request.SuggestedFileName,
            FileTypeChoices = ToFileTypes(request.Filters)
        });

        if (file is null)
        {
            return null;
        }

        return await file.OpenWriteAsync();
    }

    private static FilePickerFileType[] ToFileTypes(IReadOnlyList<ViewModels.FilePickerFilter> filters) =>
        filters
            .Select(filter => new FilePickerFileType(filter.Name)
            {
                Patterns = filter.Extensions
                    .Select(extension => extension == "*" ? "*.*" : $"*.{extension.TrimStart('.')}")
                    .ToArray()
            })
            .ToArray();
}
