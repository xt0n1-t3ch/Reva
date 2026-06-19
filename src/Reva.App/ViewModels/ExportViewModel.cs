using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reva.App.Services;
using Reva.Core.Contracts;
using Reva.Core.Export;
using Reva.Core.Reinsurance;

namespace Reva.App.ViewModels;

public sealed record OpenFileRequest(string Title, IReadOnlyList<FilePickerFilter> Filters);

public sealed record SaveFileRequest(string Title, string SuggestedFileName, IReadOnlyList<FilePickerFilter> Filters);

public sealed record FilePickerFilter(string Name, IReadOnlyList<string> Extensions);

public sealed record OpenFileResult(string Path, Stream Content);

public partial class ExportViewModel : ViewModelBase
{
    private const string TemplateFileExtension = "json";
    private const string ImportedTemplateSuffix = " (imported)";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyList<FilePickerFilter> TemplateFilters =
    [
        new FilePickerFilter("Reva export template", [TemplateFileExtension]),
        new FilePickerFilter("All files", ["*"])
    ];

    private readonly IRevaClient _client;

    [ObservableProperty]
    private string _title = "Export";

    [ObservableProperty]
    private string _description = "Customizable export templates with live preview to CSV, Excel, and JSON.";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTemplates))]
    [NotifyPropertyChangedFor(nameof(IsTemplateListEmpty))]
    private int _templateCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(SaveTemplateCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteTemplateCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateTemplateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportTemplateFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddColumnCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviewCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocumentCommand))]
    private TemplateEditorViewModel? _selected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviewCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportDocumentCommand))]
    private DocumentOptionViewModel? _selectedDocument;

    [ObservableProperty]
    private bool _hasPreview;

    public ObservableCollection<TemplateEditorViewModel> Templates { get; } = [];

    public ObservableCollection<DocumentOptionViewModel> Documents { get; } = [];

    public ObservableCollection<string> PreviewHeaders { get; } = [];

    public ObservableCollection<PreviewRowViewModel> PreviewRows { get; } = [];

    public IReadOnlyList<ExportFormat> Formats { get; } = Enum.GetValues<ExportFormat>();

    public IReadOnlyList<string> CanonicalSources { get; } = ReinsuranceFieldNames.Canonical;

    public Func<OpenFileRequest, CancellationToken, Task<OpenFileResult?>>? OpenFileAsync { get; set; }

    public Func<SaveFileRequest, CancellationToken, Task<Stream?>>? SaveFileAsync { get; set; }

    public bool HasTemplates => TemplateCount > 0;

    public bool IsTemplateListEmpty => !IsBusy && TemplateCount == 0;

    public bool HasSelection => Selected is not null;

    public bool CanEdit => Selected is { IsBuiltIn: false };

    public ExportViewModel(IRevaClient client)
    {
        _client = client;
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        HasError = false;
        StatusMessage = null;

        try
        {
            var templates = await _client.ListTemplatesAsync(cancellationToken);
            var documents = await _client.ListDocumentsAsync(cancellationToken);

            ReplaceTemplates(templates);
            ReplaceDocuments(documents);

            Selected = Templates.FirstOrDefault();
            SelectedDocument = Documents.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Could not load export templates: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewTemplate()
    {
        var editor = TemplateEditorViewModel.CreateNew();
        Templates.Add(editor);
        TemplateCount = Templates.Count;
        Selected = editor;
        StatusMessage = "New template — set the columns and save.";
        HasError = false;
    }

    [RelayCommand(CanExecute = nameof(CanModifySelection))]
    private async Task SaveTemplateAsync(CancellationToken cancellationToken)
    {
        if (Selected is null)
        {
            return;
        }

        var draft = Selected.ToDraft();
        if (draft.Columns.Count == 0)
        {
            HasError = true;
            StatusMessage = "Add at least one column before saving.";
            return;
        }

        try
        {
            if (Selected.IsPersisted)
            {
                var updated = await _client.UpdateTemplateAsync(Selected.Id, draft, cancellationToken);
                if (updated is null)
                {
                    HasError = true;
                    StatusMessage = "Template no longer exists.";
                    return;
                }

                Selected.Apply(updated);
            }
            else
            {
                var created = await _client.CreateTemplateAsync(draft, cancellationToken);
                Selected.Apply(created);
            }

            HasError = false;
            StatusMessage = $"Saved \"{Selected.Name}\".";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Save failed: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DuplicateTemplateAsync(CancellationToken cancellationToken)
    {
        if (Selected is null)
        {
            return;
        }

        try
        {
            if (Selected.IsPersisted)
            {
                var copy = await _client.DuplicateTemplateAsync(Selected.Id, cancellationToken);
                if (copy is null)
                {
                    HasError = true;
                    StatusMessage = "Template no longer exists.";
                    return;
                }

                var editor = TemplateEditorViewModel.FromTemplate(copy);
                Templates.Add(editor);
                TemplateCount = Templates.Count;
                Selected = editor;
            }
            else
            {
                var editor = Selected.Clone();
                Templates.Add(editor);
                TemplateCount = Templates.Count;
                Selected = editor;
            }

            HasError = false;
            StatusMessage = "Duplicated template.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Duplicate failed: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifySelection))]
    private async Task DeleteTemplateAsync(CancellationToken cancellationToken)
    {
        if (Selected is null)
        {
            return;
        }

        try
        {
            if (Selected.IsPersisted)
            {
                var removed = await _client.DeleteTemplateAsync(Selected.Id, cancellationToken);
                if (!removed)
                {
                    HasError = true;
                    StatusMessage = "Built-in templates cannot be deleted.";
                    return;
                }
            }

            var index = Templates.IndexOf(Selected);
            Templates.Remove(Selected);
            TemplateCount = Templates.Count;
            Selected = Templates.Count == 0
                ? null
                : Templates[Math.Clamp(index, 0, Templates.Count - 1)];

            HasError = false;
            StatusMessage = "Template deleted.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Delete failed: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void AddColumn()
    {
        Selected?.AddColumn(CanonicalSources.Count > 0 ? CanonicalSources[0] : string.Empty);
    }

    [RelayCommand]
    private async Task ImportTemplateFileAsync(CancellationToken cancellationToken)
    {
        if (OpenFileAsync is null)
        {
            return;
        }

        try
        {
            var request = new OpenFileRequest("Import export template", TemplateFilters);
            var result = await OpenFileAsync(request, cancellationToken);
            if (result is null)
            {
                StatusMessage = "Import cancelled.";
                return;
            }

            await using var stream = result.Content;
            var portable = await JsonSerializer.DeserializeAsync<PortableTemplate>(stream, SerializerOptions, cancellationToken);
            if (portable is null || portable.Columns is null || portable.Columns.Count == 0)
            {
                HasError = true;
                StatusMessage = "The selected file is not a valid template.";
                return;
            }

            var editor = TemplateEditorViewModel.FromPortable(portable, ImportedTemplateSuffix);
            Templates.Add(editor);
            TemplateCount = Templates.Count;
            Selected = editor;
            HasError = false;
            StatusMessage = $"Imported \"{editor.Name}\". Review and save to persist.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Import cancelled.";
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Import failed: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ExportTemplateFileAsync(CancellationToken cancellationToken)
    {
        if (SaveFileAsync is null || Selected is null)
        {
            return;
        }

        try
        {
            var portable = Selected.ToPortable();
            var fileName = $"{SanitizeFileName(Selected.Name)}.{TemplateFileExtension}";
            var request = new SaveFileRequest("Export template to file", fileName, TemplateFilters);
            var destination = await SaveFileAsync(request, cancellationToken);
            if (destination is null)
            {
                StatusMessage = "Export cancelled.";
                return;
            }

            await using (destination)
            {
                await JsonSerializer.SerializeAsync(destination, portable, SerializerOptions, cancellationToken);
            }

            HasError = false;
            StatusMessage = $"Wrote template file \"{fileName}\".";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled.";
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Could not write the template file: {exception.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewAsync(CancellationToken cancellationToken)
    {
        if (Selected is null || SelectedDocument is null)
        {
            return;
        }

        if (!Selected.IsPersisted)
        {
            HasError = true;
            StatusMessage = "Save the template before previewing.";
            return;
        }

        try
        {
            var preview = await _client.PreviewExportAsync(SelectedDocument.Id, Selected.Id, cancellationToken);
            ApplyPreview(preview);
            if (preview is null)
            {
                HasError = true;
                StatusMessage = "No preview available for that document and template.";
            }
            else
            {
                HasError = false;
                StatusMessage = null;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Preview failed: {exception.Message}";
            ApplyPreview(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task ExportDocumentAsync(CancellationToken cancellationToken)
    {
        if (SaveFileAsync is null || Selected is null || SelectedDocument is null)
        {
            return;
        }

        if (!Selected.IsPersisted)
        {
            HasError = true;
            StatusMessage = "Save the template before exporting.";
            return;
        }

        try
        {
            var file = await _client.ExportAsync(SelectedDocument.Id, Selected.Id, cancellationToken);
            if (file is null)
            {
                HasError = true;
                StatusMessage = "Export produced no file.";
                return;
            }

            var request = new SaveFileRequest("Export document", file.FileName, FiltersForFormat(Selected.Format));
            var destination = await SaveFileAsync(request, cancellationToken);
            if (destination is null)
            {
                StatusMessage = "Export cancelled.";
                return;
            }

            await using (destination)
            {
                await destination.WriteAsync(file.Content, cancellationToken);
            }

            HasError = false;
            StatusMessage = $"Exported \"{file.FileName}\".";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled.";
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Export failed: {exception.Message}";
        }
    }

    partial void OnSelectedChanged(TemplateEditorViewModel? value)
    {
        ApplyPreview(null);
    }

    partial void OnSelectedDocumentChanged(DocumentOptionViewModel? value)
    {
        ApplyPreview(null);
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTemplateListEmpty));
    }

    private bool CanModifySelection() => CanEdit;

    private bool CanPreview() => Selected is not null && SelectedDocument is not null;

    private void ReplaceTemplates(IReadOnlyList<ExportTemplate> templates)
    {
        Templates.Clear();
        foreach (var template in templates)
        {
            Templates.Add(TemplateEditorViewModel.FromTemplate(template));
        }

        TemplateCount = Templates.Count;
    }

    private void ReplaceDocuments(IReadOnlyList<DocumentSummary> documents)
    {
        Documents.Clear();
        foreach (var document in documents)
        {
            Documents.Add(new DocumentOptionViewModel(document.Id, document.FileName));
        }
    }

    private void ApplyPreview(ExportPreview? preview)
    {
        PreviewHeaders.Clear();
        PreviewRows.Clear();

        if (preview is null)
        {
            HasPreview = false;
            return;
        }

        foreach (var header in preview.Headers)
        {
            PreviewHeaders.Add(header);
        }

        foreach (var row in preview.Rows)
        {
            PreviewRows.Add(new PreviewRowViewModel(row));
        }

        HasPreview = preview.Headers.Count > 0;
    }

    private static IReadOnlyList<FilePickerFilter> FiltersForFormat(ExportFormat format) => format switch
    {
        ExportFormat.Csv => [new FilePickerFilter("CSV", ["csv"]), new FilePickerFilter("All files", ["*"])],
        ExportFormat.Excel => [new FilePickerFilter("Excel workbook", ["xlsx"]), new FilePickerFilter("All files", ["*"])],
        ExportFormat.Json => [new FilePickerFilter("JSON", ["json"]), new FilePickerFilter("All files", ["*"])],
        _ => [new FilePickerFilter("All files", ["*"])]
    };

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "template";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "template" : cleaned;
    }
}

public sealed class PortableTemplate
{
    public string Name { get; set; } = string.Empty;

    public ExportFormat Format { get; set; }

    public List<PortableColumn> Columns { get; set; } = [];
}

public sealed class PortableColumn
{
    public string Header { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;
}

public partial class TemplateEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormatLabel))]
    private ExportFormat _format;

    [ObservableProperty]
    private bool _isBuiltIn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusLabel))]
    private bool _isPersisted;

    public Guid Id { get; private set; }

    public ObservableCollection<ColumnEditorViewModel> Columns { get; } = [];

    public string FormatLabel => Format.ToString().ToUpperInvariant();

    public string StatusLabel => IsBuiltIn ? "Built-in" : IsPersisted ? "Saved" : "Unsaved";

    public bool HasColumns => Columns.Count > 0;

    private TemplateEditorViewModel(Guid id, string name, ExportFormat format, bool isBuiltIn, bool isPersisted)
    {
        Id = id;
        _name = name;
        _format = format;
        _isBuiltIn = isBuiltIn;
        _isPersisted = isPersisted;
        Columns.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasColumns));
    }

    public bool IsEditable => !IsBuiltIn;

    public static TemplateEditorViewModel FromTemplate(ExportTemplate template)
    {
        var editor = new TemplateEditorViewModel(template.Id, template.Name, template.Format, template.IsBuiltIn, isPersisted: true);
        editor.LoadColumns(template.Columns.Select(column => (column.Header, column.Source)));
        return editor;
    }

    public static TemplateEditorViewModel FromPortable(PortableTemplate portable, string nameSuffix)
    {
        var name = string.IsNullOrWhiteSpace(portable.Name) ? "Imported template" : portable.Name;
        var editor = new TemplateEditorViewModel(Guid.Empty, name + nameSuffix, portable.Format, isBuiltIn: false, isPersisted: false);
        editor.LoadColumns(portable.Columns.Select(column => (column.Header, column.Source)));
        return editor;
    }

    public static TemplateEditorViewModel CreateNew()
    {
        var editor = new TemplateEditorViewModel(Guid.Empty, "New template", ExportFormat.Csv, isBuiltIn: false, isPersisted: false);
        editor.AddColumn(ReinsuranceFieldNames.Canonical[0]);
        return editor;
    }

    public TemplateEditorViewModel Clone()
    {
        var editor = new TemplateEditorViewModel(Guid.Empty, Name + " (copy)", Format, isBuiltIn: false, isPersisted: false);
        editor.LoadColumns(Columns.Select(column => (column.Header, column.Source)));
        return editor;
    }

    public void Apply(ExportTemplate template)
    {
        Id = template.Id;
        Name = template.Name;
        Format = template.Format;
        IsBuiltIn = template.IsBuiltIn;
        IsPersisted = true;
        OnPropertyChanged(nameof(IsEditable));
        LoadColumns(template.Columns.Select(column => (column.Header, column.Source)));
    }

    public ExportTemplateDraft ToDraft() =>
        new(Name, Format, [.. Columns
            .Where(column => !string.IsNullOrWhiteSpace(column.Header))
            .Select(column => new ExportColumn(column.Header.Trim(), column.Source.Trim()))]);

    public PortableTemplate ToPortable() => new()
    {
        Name = Name,
        Format = Format,
        Columns = [.. Columns.Select(column => new PortableColumn { Header = column.Header, Source = column.Source })]
    };

    public void AddColumn(string source)
    {
        Columns.Add(new ColumnEditorViewModel(this, string.Empty, source, IsEditable));
    }

    public void RemoveColumn(ColumnEditorViewModel column)
    {
        Columns.Remove(column);
    }

    public void MoveColumn(ColumnEditorViewModel column, int delta)
    {
        var index = Columns.IndexOf(column);
        if (index < 0)
        {
            return;
        }

        var target = index + delta;
        if (target < 0 || target >= Columns.Count)
        {
            return;
        }

        Columns.Move(index, target);
    }

    private void LoadColumns(IEnumerable<(string Header, string Source)> columns)
    {
        Columns.Clear();
        foreach (var (header, source) in columns)
        {
            Columns.Add(new ColumnEditorViewModel(this, header, source, IsEditable));
        }
    }
}

public partial class ColumnEditorViewModel : ViewModelBase
{
    private readonly TemplateEditorViewModel _owner;

    [ObservableProperty]
    private string _header;

    [ObservableProperty]
    private string _source;

    [ObservableProperty]
    private bool _isEditable;

    public IReadOnlyList<string> Sources { get; } = ReinsuranceFieldNames.Canonical;

    public ColumnEditorViewModel(TemplateEditorViewModel owner, string header, string source, bool isEditable)
    {
        _owner = owner;
        _header = header;
        _source = source;
        _isEditable = isEditable;
    }

    [RelayCommand]
    private void MoveUp() => _owner.MoveColumn(this, -1);

    [RelayCommand]
    private void MoveDown() => _owner.MoveColumn(this, 1);

    [RelayCommand]
    private void Remove() => _owner.RemoveColumn(this);
}

public sealed class DocumentOptionViewModel
{
    public DocumentOptionViewModel(Guid id, string fileName)
    {
        Id = id;
        FileName = string.IsNullOrWhiteSpace(fileName) ? id.ToString() : fileName;
    }

    public Guid Id { get; }

    public string FileName { get; }
}

public sealed class PreviewRowViewModel
{
    public PreviewRowViewModel(IReadOnlyList<string> cells)
    {
        Cells = cells;
    }

    public IReadOnlyList<string> Cells { get; }
}
