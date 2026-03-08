using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    public ObservableCollection<TopicListDto> Topics { get; } = [];
    
    public async Task InitializeWorkspaceAsync()
    {
        await GetTopicsAsync();
    }
    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await GetTopicsAsync();
    }
    [RelayCommand]
    public async Task CreateTopicAsync(Guid? parentId = null)
    {
        var formVm = new CreateTopicViewModel(parentId, gateway);
        
        var result = await layerProxy.OpenAsync<CreateTopicViewModel, Result>(formVm, LayerPresentation.Modal);
        if (result is { IsSuccess: true })
        {
            await RefreshWorkspaceAsync();
        }
    }
    private async Task GetTopicsAsync()
    {
        var result = await gateway.GetTopicsAsync();
        if (result is not null)
        {
            Topics.Clear();
            var sortedTopics = BuildTopicTree(result);
            
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                foreach (var topic in sortedTopics)
                {
                    Topics.Add(topic);   
                }
            });
        }
    }

    private static List<TopicListDto> BuildTopicTree(List<TopicListDto> topics)
    {
        var parentlessTopics = topics.Where(t => t.ParentId is null).ToList();
        
        foreach (var parentlessTopic in parentlessTopics)
        {
            PopulateChildren(parentlessTopic, topics);
        }
        
        return parentlessTopics;
    }

    private static TopicListDto PopulateChildren(TopicListDto rootTopic, List<TopicListDto> topics)
    {
        var children = topics
            .Where(t => t.ParentId == rootTopic.Id)
            .Select(t => PopulateChildren(t, topics))
            .ToList();

        foreach (var child in children)
        {
            rootTopic.Children.Add(child);
        }

        return rootTopic;
    }
}