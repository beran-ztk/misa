using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Details.Information.Base;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;

namespace Misa.Ui.Avalonia.Features.Common.Deadline;

public sealed partial class DeadlineSectionViewModel : ViewModelBase
{
    private DetailInformationViewModel Parent { get; }
    public DeadlineSectionViewModel(DetailInformationViewModel parent)
    {
        Parent = parent;

        Parent.WhenAnyValue(x => x.Parent.Deadline)
            .Subscribe(_ =>
            {
                OnPropertyChanged(nameof(HasDeadline));
                OnPropertyChanged(nameof(DeadlineText));
                OnPropertyChanged(nameof(PrimaryButtonText));
                OnPropertyChanged(nameof(ShowAddIcon));
                OnPropertyChanged(nameof(ShowEditIcon));
            });
    }

    public DeadlineInputViewModel Input { get; } = new();

    public bool HasDeadline => Parent.Parent.Deadline.DueAtUtc is not null;
    public bool ShowAddIcon => !IsEditOpen && !HasDeadline;

    public bool ShowEditIcon => !IsEditOpen && HasDeadline;

    public string DeadlineText
        => HasDeadline
            ? Parent.Parent.Deadline!.DueAtUtc!.Value.ToString("yyyy-MM-dd HH:mm 'UTC'")
            : "No deadline set.";

    public string PrimaryButtonText
        => IsEditOpen ? "Cancel" : (HasDeadline ? "Edit" : "Add");

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(PrimaryButtonText))]
    [NotifyPropertyChangedFor(nameof(ShowAddIcon))]
    [NotifyPropertyChangedFor(nameof(ShowEditIcon))]
    private bool _isEditOpen;

    [RelayCommand]
    private void ToggleEdit()
    {
        if (IsEditOpen)
        {
            IsEditOpen = false;
            Input.Reset();
            return;
        }

        IsEditOpen = true;
        Input.IsEnabled = true;
        Input.CreateWithParent = false;
    }

    [RelayCommand]
    private async Task Save()
    {
        var deadline = Input.ToDtoOrNull();
        if (deadline is null)
            return;

        var dto = new CreateOnceScheduleDto(
            TargetItemId: Parent.Parent.Item.Id,
            DueAtUtc: deadline.DueAtUtc
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, "scheduling/once")
        {
            Content = JsonContent.Create(dto)
        };

        using var response = await Parent.Parent.NavigationService.NavigationStore.HttpClient
            .SendAsync(request, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        // Refresh
        await Parent.Parent.Reload();
        IsEditOpen = false;
        Input.Reset();
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (!HasDeadline)
            return;

        var targetId = Parent.Parent.Item.Id;

        using var response = await Parent.Parent.NavigationService.NavigationStore.HttpClient
            .DeleteAsync($"scheduling/once/{targetId}", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        await Parent.Parent.Reload();
        IsEditOpen = false;
        Input.Reset();
    }
}
