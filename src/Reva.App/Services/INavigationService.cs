using System;
using Reva.App.ViewModels;

namespace Reva.App.Services;

public interface IDocumentNavigationTarget
{
    void RequestDocument(Guid documentId);
}

public interface INavigationService
{
    ViewModelBase? Current { get; }

    string? CurrentRoute { get; }

    event Action<ViewModelBase>? CurrentChanged;

    void NavigateTo(string route);

    void OpenDocument(Guid documentId);
}
