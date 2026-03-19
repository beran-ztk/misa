using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Ui.Avalonia.Common.Converters;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Chronicle;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public sealed partial class CreateJournalViewModel(ChronicleGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{

    [ObservableProperty] private string _journalTitle = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private DateTimeOffset _occurredAtDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private TimeSpan _occurredAtTime = DateTimeOffset.Now.TimeOfDay;

    [ObservableProperty] private DateTimeOffset? _untilAtDate;
    [ObservableProperty] private TimeSpan? _untilAtTime;
    
    partial void OnUntilAtDateChanged(DateTimeOffset? value)
    {
        if (value is not null && UntilAtTime is null)
            UntilAtTime = TimeSpan.Zero;
    }

    public string FormTitle { get; } = "Create Journal";
    public string? FormDescription { get; }

    public async Task<Result<Result>> SubmitAsync()
    {
        var dto = new CreateJournalRequest(
            JournalTitle, 
            Description, 
            DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(OccurredAtDate, OccurredAtTime) ?? throw new NullReferenceException(), 
            DateTimeOffsetHelper.CombineLocalDateAndTimeToUtc(UntilAtDate, UntilAtTime));
        
        var result = await gateway.CreateAsync(dto);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}
