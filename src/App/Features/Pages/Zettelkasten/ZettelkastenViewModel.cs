using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    public ObservableCollection<TopicListDto> Topics { get; } = [];
    
    [ObservableProperty] private string? _topicTitle;
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
    private async Task CreateTopicAsync()
    {
        if (TopicTitle is null) return;
        var request = new CreateTopicRequest(TopicTitle, null);
        await gateway.CreateTopicAsync(request);
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