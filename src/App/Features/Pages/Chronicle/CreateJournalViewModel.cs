using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Ui.Avalonia.Common.Converters;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public sealed partial class CreateJournalViewModel(ChronicleGateway gateway) : ObservableObject, IHostedForm
{
    public string Title => "Create new Journal";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    [ObservableProperty] private string _journalTitle = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private DateTimeOffset _occurredAtDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private TimeSpan _occurredAtTime = DateTimeOffset.Now.TimeOfDay;

    [ObservableProperty] private DateTimeOffset? _untilAtDate;
    [ObservableProperty] private TimeSpan? _untilAtTime;
    
    public async Task<Result> SubmitAsync()
    {
        var dto = new CreateJournalRequest(
            JournalTitle, 
            Description, 
            DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(OccurredAtDate, OccurredAtTime) ?? throw new NullReferenceException(), 
            DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(UntilAtDate, UntilAtTime));
        return await gateway.CreateAsync(dto);
    }
}
