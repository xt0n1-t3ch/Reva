using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reva.App.Services;
using Reva.Core.Documents;
using Reva.Infrastructure.Agent;

namespace Reva.App.ViewModels;

public enum UploadFileStatus
{
    Pending,
    Uploading,
    Done,
    Warning,
    Failed,
}

public partial class UploadFileEntry : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private UploadFileStatus _status = UploadFileStatus.Pending;

    [ObservableProperty]
    private string _statusText = "Pending";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isUnsupported;
}

public partial class UploadViewModel : ViewModelBase
{
    private readonly IRevaClient _client;
    private readonly IAppActionBus _actionBus;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private bool _isDragOver;

    [ObservableProperty]
    private bool _hasEntries;

    [ObservableProperty]
    private bool _isUploading;

    public ObservableCollection<UploadFileEntry> Entries { get; } = [];

    public UploadViewModel(IRevaClient client, IAppActionBus actionBus)
    {
        _client = client;
        _actionBus = actionBus;
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var done = Entries.Where(e => e.Status is UploadFileStatus.Done or UploadFileStatus.Failed).ToList();
        foreach (var entry in done)
        {
            Entries.Remove(entry);
        }
        HasEntries = Entries.Count > 0;
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    public async Task AcceptDropAsync(string[] paths, CancellationToken externalToken = default)
    {
        if (paths.Length == 0)
        {
            return;
        }

        await EnqueueAndUploadAsync(paths, externalToken);
    }

    internal async Task BrowseAndUploadAsync(string[] paths, CancellationToken externalToken = default)
    {
        if (paths.Length == 0)
        {
            return;
        }

        await EnqueueAndUploadAsync(paths, externalToken);
    }

    private async Task EnqueueAndUploadAsync(string[] paths, CancellationToken externalToken)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var token = _cts.Token;

        var entries = paths.Select(p =>
        {
            var ext = Path.GetExtension(p).ToLowerInvariant();
            var unsupported = !AcceptedDocumentExtensions.IsAccepted(ext);
            return new UploadFileEntry
            {
                FileName = Path.GetFileName(p),
                Status = UploadFileStatus.Pending,
                StatusText = unsupported ? "Unsupported type — will attempt" : "Pending",
                IsUnsupported = unsupported,
            };
        }).ToList();

        Dispatcher.UIThread.Post(() =>
        {
            foreach (var entry in entries)
            {
                Entries.Add(entry);
            }
            HasEntries = true;
            IsUploading = true;
        });

        var any = false;

        for (var i = 0; i < paths.Length; i++)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            var path = paths[i];
            var entry = entries[i];

            Dispatcher.UIThread.Post(() =>
            {
                entry.Status = UploadFileStatus.Uploading;
                entry.StatusText = "Uploading…";
                entry.Progress = 0;
            });

            try
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (!DocumentContentTypes.Map.TryGetValue(ext, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                var fileName = Path.GetFileName(path);

                await using var stream = File.OpenRead(path);

                Dispatcher.UIThread.Post(() => entry.Progress = 10);

                await _client.UploadAsync(fileName, contentType, stream, token);

                Dispatcher.UIThread.Post(() =>
                {
                    entry.Status = entry.IsUnsupported ? UploadFileStatus.Warning : UploadFileStatus.Done;
                    entry.StatusText = entry.IsUnsupported ? "Done (unsupported type)" : "Done";
                    entry.Progress = 100;
                });

                any = true;
            }
            catch (OperationCanceledException)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    entry.Status = UploadFileStatus.Failed;
                    entry.StatusText = "Cancelled";
                    entry.Progress = 0;
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message.Length > 80 ? ex.Message[..80] : ex.Message;
                Dispatcher.UIThread.Post(() =>
                {
                    entry.Status = UploadFileStatus.Failed;
                    entry.StatusText = "Failed: " + msg;
                    entry.Progress = 0;
                });
            }
        }

        Dispatcher.UIThread.Post(() => IsUploading = false);

        if (any)
        {
            _actionBus.Publish(new AppAction(AppActionKind.Refresh));
        }
    }
}
