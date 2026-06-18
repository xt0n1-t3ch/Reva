using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Reva.App.ViewModels;
using Reva.Core.Documents;

namespace Reva.App.Views;

public sealed class UploadStatusBrushConverter : IValueConverter
{
    public static readonly UploadStatusBrushConverter Instance = new();

    private static readonly IBrush Pending = new SolidColorBrush(Color.FromRgb(0x8A, 0x95, 0xAD));
    private static readonly IBrush Uploading = new SolidColorBrush(Color.FromRgb(0x4F, 0x7D, 0xF9));
    private static readonly IBrush Done = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
    private static readonly IBrush Warning = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly IBrush Failed = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is UploadFileStatus status ? status switch
        {
            UploadFileStatus.Uploading => Uploading,
            UploadFileStatus.Done => Done,
            UploadFileStatus.Warning => Warning,
            UploadFileStatus.Failed => Failed,
            _ => Pending,
        } : Pending;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class UploadProgressVisibleConverter : IValueConverter
{
    public static readonly UploadProgressVisibleConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is UploadFileStatus.Uploading;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public partial class UploadControl : UserControl
{
    private static readonly FilePickerFileType DocumentFilter = new("Documents")
    {
        Patterns = AcceptedDocumentExtensions.All.Select(e => "*" + e).ToArray(),
    };

    public UploadControl()
    {
        InitializeComponent();
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (DataContext is UploadViewModel vm && e.DataTransfer.Contains(DataFormat.File))
        {
            vm.IsDragOver = true;
        }
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (DataContext is UploadViewModel vm)
        {
            vm.IsDragOver = false;
        }
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not UploadViewModel vm)
        {
            return;
        }

        vm.IsDragOver = false;

        var paths = e.DataTransfer.TryGetFiles()?
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .ToArray();

        if (paths is { Length: > 0 })
        {
            _ = vm.AcceptDropAsync(paths);
        }

        e.Handled = true;
    }

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not UploadViewModel vm)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select documents",
            AllowMultiple = true,
            FileTypeFilter = [DocumentFilter],
        });

        if (files.Count == 0)
        {
            return;
        }

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .ToArray();

        await vm.BrowseAndUploadAsync(paths);
    }
}
