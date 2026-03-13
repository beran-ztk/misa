using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten.Forms;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel(ZettelkastenGateway gateway, LayerProxy layerProxy) : ViewModelBase
{
    public ObservableCollection<KnowledgeIndexEntryDto> Index { get; } = [];
    public List<ZettelDto> Zettels { get; private set; } = [];

    [ObservableProperty] private ZettelDto? _selectedZettel;
    [ObservableProperty] private string? _editableContent;
    [ObservableProperty] private string _saveStateLabel = string.Empty;

    private bool _suppressContentSave;
    private CancellationTokenSource? _saveCts;

    public async Task InitializeWorkspaceAsync()
    {
        await LoadIndexAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await LoadIndexAsync();
    }

    partial void OnSelectedZettelChanged(ZettelDto? value)
    {
        _suppressContentSave = true;
        EditableContent = value?.Content;
        SaveStateLabel = string.Empty;
        _suppressContentSave = false;
    }

    partial void OnEditableContentChanged(string? value)
    {
        if (_suppressContentSave) return;
        if (SelectedZettel is null) return;

        SaveStateLabel = "Unsaved";
        _ = AutoSaveAsync(SelectedZettel.Id, value);
    }

    private async Task AutoSaveAsync(Guid zettelId, string? content)
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        try
        {
            await Task.Delay(1000, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (SelectedZettel?.Id != zettelId) return;

        var result = await gateway.UpdateZettelContentAsync(zettelId, content);

        if (result.IsSuccess)
        {
            // Update the cached ZettelDto
            var idx = Zettels.FindIndex(z => z.Id == zettelId);
            if (idx >= 0)
                Zettels[idx] = Zettels[idx] with { Content = content };

            if (SelectedZettel?.Id == zettelId)
                SaveStateLabel = "Saved";
        }
        else
        {
            if (SelectedZettel?.Id == zettelId)
                SaveStateLabel = "Save failed";
        }
    }

    public async Task CreateZettelAsync(Guid? topicId = null, string? topicName = null)
    {
        string description = topicId.HasValue
            ? $"This Zettel will be created under the topic '{topicName}'."
            : "Select a topic node to create a Zettel.";

        var formVm = new CreateZettelViewModel(topicId, description, gateway);
        var result = await layerProxy.OpenAsync<CreateZettelViewModel, Result>(formVm, LayerPresentation.Modal);
        if (result is { IsSuccess: true })
        {
            await RefreshWorkspaceAsync();
        }
    }

    public async Task CreateTopicAsync(Guid? parentId = null, string? parentName = null)
    {
        string description = parentId.HasValue
            ? $"This topic will be assigned under the topic '{parentName}'."
            : "This topic will be on root-level.";

        var formVm = new CreateTopicViewModel(parentId, description, gateway);

        var result = await layerProxy.OpenAsync<CreateTopicViewModel, Result>(formVm, LayerPresentation.Modal);
        if (result is { IsSuccess: true })
        {
            await RefreshWorkspaceAsync();
        }
    }

    private async Task LoadIndexAsync()
    {
        var indexResult = await gateway.GetKnowledgeIndexAsync();
        var zettelResult = await gateway.GetZettelsAsync();

        if (indexResult is not null)
        {
            var tree = BuildIndexTree(indexResult);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Index.Clear();
                foreach (var entry in tree)
                    Index.Add(entry);

                SelectedZettel = null;
            });
        }

        if (zettelResult is not null)
            Zettels = zettelResult;
    }

    private static List<KnowledgeIndexEntryDto> BuildIndexTree(List<KnowledgeIndexEntryDto> entries)
    {
        var roots = entries
            .Where(e => e.ParentId is null)
            .OrderBy(e => e.Workflow)
            .ThenBy(e => e.Title)
            .ToList();

        foreach (var root in roots)
            PopulateChildren(root, entries);

        return roots;
    }

    private static KnowledgeIndexEntryDto PopulateChildren(KnowledgeIndexEntryDto entry, List<KnowledgeIndexEntryDto> all)
    {
        var children = all
            .Where(e => e.ParentId == entry.Id)
            .OrderBy(e => e.Workflow)
            .ThenBy(e => e.Title)
            .Select(e => PopulateChildren(e, all))
            .ToList();

        foreach (var child in children)
            entry.Children.Add(child);

        return entry;
    }
}
