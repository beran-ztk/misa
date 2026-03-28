using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Converters;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Relations;
using Misa.Ui.Avalonia.Features.Utilities.Toast;


namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel : ViewModelBase
{
    public InspectorFacadeViewModel Facade { get; }
    public InspectorRelationsViewModel Relations { get; }
    private CancellationTokenSource? _elapsedCts;
    private Task? _elapsedLoop;
    public InspectorEntryViewModel(InspectorFacadeViewModel facade)
    {
        Facade = facade;
        Facade.State.PropertyChanged += OnFacadeStatePropertyChanged;
        Relations = new InspectorRelationsViewModel(facade);
    }

    [RelayCommand]
    private async Task ArchiveItem()
    {
        await EnsureSessionEndedAsync();
        // var result = await Facade.Gateway.ArchiveAsync(Facade.State.Item.Id);
        // if (result.IsSuccess)
            // Facade.ContextState.NotifyRemoved();
    }

    [RelayCommand]
    private async Task DeleteItem()
    {
        // await EnsureSessionEndedAsync();
        // var result = await Facade.Gateway.DeleteAsync(Facade.State.Item.Id);
        // if (result.IsSuccess)
        // {
        //     Facade.LayerProxy.ShowActionToast("Task deleted", type: ToastType.Info);
        //     Facade.ContextState.NotifyRemoved();
        // }
    }

    /// <summary>
    /// Silently stops any active session before an archive or delete operation
    /// to prevent leaving orphaned running sessions on inactive items.
    /// </summary>
    private async Task EnsureSessionEndedAsync()
    {
        if (CurrentSession == null) return;

        var dto = new StopSessionDto(
            Facade.State.Item.Id,
            SessionEfficiencyDto.None,
            SessionConcentrationDto.None,
            "Was automatically stopped because of archiving/deleting.");

        // await Facade.Gateway.EndSessionAsync(dto);
    }
    private void OnFacadeStatePropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Facade.State.Item):
                CurrentItemPropertyChanged();
                CurrentSessionPropertyChanged();
                DeadlinePropertyChanged();
                break;
        }
    }
    private void CurrentItemPropertyChanged()
    {
        Facade.State.IsEditItemFormOpen = false;
        OnPropertyChanged(nameof(OverviewTitle));
    }
    private void CurrentSessionPropertyChanged()
    {
        OnPropertyChanged(nameof(CurrentSession));
        OnPropertyChanged(nameof(HasActiveSession));

        OnPropertyChanged(nameof(CanStartSession));
        OnPropertyChanged(nameof(CanPauseSession));
        OnPropertyChanged(nameof(CanContinueSession));
        OnPropertyChanged(nameof(CanEndSession));

        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(SessionStateLabel));
        OnPropertyChanged(nameof(ActiveSessionObjective));
        OnPropertyChanged(nameof(HasActiveSessionObjective));
        OnPropertyChanged(nameof(ActiveSessionSegmentLabel));

        OnPropertyChanged(nameof(ActiveSessionElapsedDisplay));
        OnPropertyChanged(nameof(ActiveSessionPlannedSuffixDisplay));
        OnPropertyChanged(nameof(IsOverPlannedDuration));
        OnPropertyChanged(nameof(ActiveSessionSegmentDisplay));

        RunElapsedLoop();
    }
    private void DeadlinePropertyChanged()
    {
        OnPropertyChanged(nameof(Deadline));
        OnPropertyChanged(nameof(HasDeadline));
        OnPropertyChanged(nameof(DeadlineDisplay));
    }
    private void RunElapsedLoop()
    {
        if (HasActiveSession)
            StartElapsedLoop();
        else
            StopElapsedLoop();
    }
    private void StartElapsedLoop()
    {
        if (_elapsedLoop is { IsCompleted: false })
            return;

        _elapsedCts?.Cancel();
        _elapsedCts?.Dispose();

        _elapsedCts = new CancellationTokenSource();
        var token = _elapsedCts.Token;

        _elapsedLoop = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested && HasActiveSession)
                {
                    OnPropertyChanged(nameof(ActiveSessionElapsedDisplay));
                    OnPropertyChanged(nameof(IsOverPlannedDuration));
                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }, token);
    }

    private void StopElapsedLoop()
    {
        _elapsedCts?.Cancel();
        _elapsedCts?.Dispose();
        _elapsedCts = null;
        _elapsedLoop = null;
    }
    
    private TimeSpan? ElapsedTime() =>
        CurrentSession?.Segments.Aggregate(TimeSpan.Zero, (sum, s) =>
        {
            var end = s.EndedAtUtc ?? DateTimeOffset.UtcNow;
            return sum + (end - s.StartedAtUtc);
        });
    
    public string ActiveSessionElapsedDisplay =>
        TimeSpanCalculator.FormatDuration(ElapsedTime());

    public string ActiveSessionPlannedSuffixDisplay =>
        CurrentSession?.PlannedDuration is not null
            ? $" / {TimeSpanCalculator.FormatDuration(CurrentSession.PlannedDuration)}"
            : string.Empty;

    public bool IsOverPlannedDuration =>
        CurrentSession?.PlannedDuration is { } planned && ElapsedTime() > planned;
}