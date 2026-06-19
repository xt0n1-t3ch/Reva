using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Reva.App.ViewModels;
using Reva.Core.Documents;

namespace Reva.App.Views;

public sealed class UploadStatusBrushConverter : IValueConverter
{
    private static readonly IBrush Fallback = new SolidColorBrush(Color.FromRgb(0x6B, 0x6A, 0x73));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var token = value is UploadFileStatus status
            ? status switch
            {
                UploadFileStatus.Uploading => "AccentBrush",
                UploadFileStatus.Done => "SuccessBrush",
                UploadFileStatus.Warning => "WarningBrush",
                UploadFileStatus.Failed => "DangerBrush",
                _ => "MutedForegroundBrush",
            }
            : "MutedForegroundBrush";

        return ResolveBrush(token);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static IBrush ResolveBrush(string token)
    {
        if (Application.Current is { } application)
        {
            var theme = application.ActualThemeVariant ?? ThemeVariant.Light;
            if (application.TryGetResource(token, theme, out var resource) && resource is IBrush brush)
            {
                return brush;
            }
        }

        return Fallback;
    }
}

public sealed class UploadProgressVisibleConverter : IValueConverter
{
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

        if (DataContext is null &&
            (Application.Current as App)?.Services is { } services)
        {
            DataContext = services.GetService<UploadViewModel>();
        }
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
