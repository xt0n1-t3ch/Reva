using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reva.App.Services;
using Reva.Core.Contracts;
using Reva.Core.Reinsurance;

namespace Reva.App.ViewModels;

public partial class MappingsViewModel : ViewModelBase
{
    private const string SystemReviewer = "Mappings console";
    private const string MappingCorrectionDecision = "MappingCorrection";
    private const string LearnedSource = "Learned";
    private const string CorrectedSource = "Corrected";
    private const string AllSendersFilter = "All senders";

    private readonly IRevaClient _client;
    private readonly List<MappingRowViewModel> _allMappings = [];

    [ObservableProperty]
    private string _title = "Mappings";

    [ObservableProperty]
    private string _description = "Sender schema mappings the system has learned, with overrides and confidence.";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMappings))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private int _mappingCount;

    [ObservableProperty]
    private string _senderFilter = AllSendersFilter;

    [ObservableProperty]
    private int _senderCount;

    public ObservableCollection<MappingRowViewModel> Mappings { get; } = [];

    public ObservableCollection<string> SenderFilters { get; } = [];

    public bool HasMappings => MappingCount > 0;

    public bool IsEmpty => !IsBusy && MappingCount == 0;

    public MappingsViewModel(IRevaClient client)
    {
        _client = client;
        SenderFilters.Add(AllSendersFilter);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        HasError = false;
        StatusMessage = null;

        try
        {
            var documents = await _client.ListDocumentsAsync(cancellationToken);
            var aggregated = new Dictionary<string, MappingRowViewModel>(StringComparer.OrdinalIgnoreCase);
            var senders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var summary in documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var detail = await _client.GetDocumentAsync(summary.Id, cancellationToken);
                if (detail is null)
                {
                    continue;
                }

                foreach (var mapping in detail.SchemaMappings)
                {
                    if (string.IsNullOrWhiteSpace(mapping.SourceHeader))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(mapping.SenderKey))
                    {
                        senders.Add(mapping.SenderKey);
                    }

                    var key = BuildKey(mapping.SenderKey, mapping.SourceHeader);
                    if (aggregated.TryGetValue(key, out var existing))
                    {
                        existing.Merge(detail.Id, mapping);
                        continue;
                    }

                    aggregated[key] = new MappingRowViewModel(detail.Id, mapping, PersistCorrectionAsync);
                }
            }

            ReplaceFilters(senders);
            ReplaceMappings(aggregated.Values
                .OrderBy(row => row.Sender, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.SourceHeader, StringComparer.OrdinalIgnoreCase));

            StatusMessage = MappingCount == 0
                ? "No learned mappings yet. They appear as documents are reviewed."
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            HasError = true;
            StatusMessage = $"Could not load mappings: {exception.Message}";
            ReplaceMappings([]);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

    partial void OnSenderFilterChanged(string value) => ApplyFilter();

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task<bool> PersistCorrectionAsync(MappingRowViewModel row, string canonicalField, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(canonicalField))
        {
            return false;
        }

        var decision = new ReviewDecision(MappingCorrectionDecision, SystemReviewer, null, [])
        {
            MappingCorrections = [new SchemaMappingCorrection(row.SourceHeader, canonicalField)]
        };

        try
        {
            var updated = await _client.SaveReviewAsync(row.DocumentId, decision, cancellationToken);
            return updated is not null;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private void ReplaceFilters(SortedSet<string> senders)
    {
        var selected = SenderFilter;
        SenderFilters.Clear();
        SenderFilters.Add(AllSendersFilter);
        foreach (var sender in senders)
        {
            SenderFilters.Add(sender);
        }

        SenderCount = senders.Count;

        if (!SenderFilters.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            SenderFilter = AllSendersFilter;
        }
    }

    private void ReplaceMappings(IEnumerable<MappingRowViewModel> rows)
    {
        _allMappings.Clear();
        _allMappings.AddRange(rows);
        MappingCount = _allMappings.Count;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var all = string.Equals(SenderFilter, AllSendersFilter, StringComparison.OrdinalIgnoreCase);

        Mappings.Clear();
        foreach (var row in _allMappings)
        {
            if (all || string.Equals(row.Sender, SenderFilter, StringComparison.OrdinalIgnoreCase))
            {
                Mappings.Add(row);
            }
        }
    }

    private static string BuildKey(string? senderKey, string sourceHeader) =>
        $"{senderKey?.Trim().ToLowerInvariant()}{sourceHeader.Trim().ToLowerInvariant()}";

    public static string ResolveOriginLabel(bool isCorrected, bool isLearned, string source)
    {
        if (isCorrected)
        {
            return CorrectedSource;
        }

        if (isLearned)
        {
            return LearnedSource;
        }

        return string.IsNullOrWhiteSpace(source) ? "Inferred" : source;
    }
}

public partial class MappingRowViewModel : ViewModelBase
{
    private readonly Func<MappingRowViewModel, string, CancellationToken, Task<bool>> _persist;
    private bool _suppressPersist;

    [ObservableProperty]
    private string _sender;

    [ObservableProperty]
    private string _sourceHeader;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OriginLabel))]
    private string _canonicalField;

    [ObservableProperty]
    private double _confidence;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OriginLabel))]
    private bool _isLearned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OriginLabel))]
    private bool _isCorrected;

    [ObservableProperty]
    private string _origin;

    [ObservableProperty]
    private int _documentCount = 1;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _saveError;

    public Guid DocumentId { get; private set; }

    public string ConfidenceDisplay => Confidence.ToString("P0", System.Globalization.CultureInfo.CurrentCulture);

    public double ConfidencePercent => Math.Clamp(Confidence, 0d, 1d) * 100d;

    public string OriginLabel => MappingsViewModel.ResolveOriginLabel(IsCorrected, IsLearned, Origin);

    public IReadOnlyList<string> CanonicalFields { get; } = ReinsuranceFieldNames.Canonical;

    public MappingRowViewModel(
        Guid documentId,
        SchemaMapping mapping,
        Func<MappingRowViewModel, string, CancellationToken, Task<bool>> persist)
    {
        _persist = persist;
        DocumentId = documentId;
        _sender = string.IsNullOrWhiteSpace(mapping.SenderKey) ? "Unattributed" : mapping.SenderKey;
        _sourceHeader = mapping.SourceHeader;
        _canonicalField = ResolveCanonical(mapping.CanonicalField);
        _confidence = mapping.Confidence;
        _isLearned = mapping.IsLearned;
        _isCorrected = mapping.IsCorrected;
        _origin = mapping.Source;
    }

    public void Merge(Guid documentId, SchemaMapping mapping)
    {
        DocumentCount++;
        if (mapping.Confidence > Confidence)
        {
            DocumentId = documentId;
            _suppressPersist = true;
            CanonicalField = ResolveCanonical(mapping.CanonicalField);
            _suppressPersist = false;
            Confidence = mapping.Confidence;
            Origin = mapping.Source;
        }

        IsLearned |= mapping.IsLearned;
        IsCorrected |= mapping.IsCorrected;
        OnPropertyChanged(nameof(ConfidenceDisplay));
        OnPropertyChanged(nameof(ConfidencePercent));
    }

    partial void OnConfidenceChanged(double value)
    {
        OnPropertyChanged(nameof(ConfidenceDisplay));
        OnPropertyChanged(nameof(ConfidencePercent));
    }

    async partial void OnCanonicalFieldChanged(string value)
    {
        if (_suppressPersist || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        IsSaving = true;
        SaveError = null;
        var saved = await _persist(this, value, CancellationToken.None);
        IsSaving = false;

        if (saved)
        {
            IsCorrected = true;
        }
        else
        {
            SaveError = "Override could not be saved.";
        }
    }

    private static string ResolveCanonical(string canonicalField)
    {
        if (string.IsNullOrWhiteSpace(canonicalField))
        {
            return ReinsuranceFieldNames.Canonical[0];
        }

        foreach (var field in ReinsuranceFieldNames.Canonical)
        {
            if (string.Equals(field, canonicalField, StringComparison.OrdinalIgnoreCase))
            {
                return field;
            }
        }

        return canonicalField;
    }
}
