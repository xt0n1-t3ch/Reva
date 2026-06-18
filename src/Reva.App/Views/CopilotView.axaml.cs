using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using Reva.App.ViewModels;

namespace Reva.App.Views;

public partial class CopilotView : UserControl
{
    private CopilotViewModel? _trackedViewModel;
    private bool _followStream;

    public CopilotView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Track(DataContext as CopilotViewModel);
        if (_trackedViewModel is { } viewModel)
        {
            _ = viewModel.RefreshStatusAsync();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        Track(DataContext as CopilotViewModel);
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Track(null);
    }

    private void Track(CopilotViewModel? viewModel)
    {
        if (ReferenceEquals(_trackedViewModel, viewModel))
        {
            return;
        }

        if (_trackedViewModel is not null)
        {
            _trackedViewModel.Messages.CollectionChanged -= OnMessagesChanged;
            _trackedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _trackedViewModel = viewModel;

        if (_trackedViewModel is not null)
        {
            _trackedViewModel.Messages.CollectionChanged += OnMessagesChanged;
            _trackedViewModel.PropertyChanged += OnViewModelPropertyChanged;
            _followStream = _trackedViewModel.IsBusy;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotViewModel.IsBusy) && _trackedViewModel is { } viewModel)
        {
            _followStream = viewModel.IsBusy;
            if (_followStream)
            {
                ScrollToEnd();
            }
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScrollToEnd();

    private void OnScrollLayoutUpdated(object? sender, EventArgs e)
    {
        if (_followStream)
        {
            ScrollToEnd();
        }
    }

    private void ScrollToEnd()
    {
        if (MessageScroll is { } scroll)
        {
            Dispatcher.UIThread.Post(scroll.ScrollToEnd, DispatcherPriority.Background);
        }
    }
}
