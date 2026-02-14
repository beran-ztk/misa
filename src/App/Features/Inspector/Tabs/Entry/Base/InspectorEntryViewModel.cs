using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Misa.Ui.Avalonia.Common.Formatting;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel : ViewModelBase
{
    public InspectorFacadeViewModel Facade { get; }
    private CancellationTokenSource? _elapsedCts;
    private Task? _elapsedLoop;
    public InspectorEntryViewModel(InspectorFacadeViewModel facade)
    {
        Facade = facade;
        Facade.State.PropertyChanged += OnFacadeStatePropertyChanged;
    }
    private void OnFacadeStatePropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Facade.State.CurrentSessionOverview))
            return;

        CurrentSessionPropertyChanged();
    }

    private void CurrentSessionPropertyChanged()
    {
        OnPropertyChanged(nameof(CurrentSession));
        OnPropertyChanged(nameof(HasActiveSession));

        OnPropertyChanged(nameof(CanStartSession));
        OnPropertyChanged(nameof(CanPauseSession));
        OnPropertyChanged(nameof(CanContinueSession));
        OnPropertyChanged(nameof(CanEndSession));

        OnPropertyChanged(nameof(ActiveSessionElapsedDisplay));
        OnPropertyChanged(nameof(ActiveSessionSegmentDisplay));

        RunElapsedLoop();
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
    
    public string ActiveSessionElapsedDisplay
    {
        get
        {
            var baseText = DurationFormatter.FormatDuration(ElapsedTime());

            var plannedText = CurrentSession?.PlannedDuration is not null
                ? $" / {DurationFormatter.FormatDuration(CurrentSession.PlannedDuration)}"
                : string.Empty;

            return baseText + plannedText;
        }
    }
}