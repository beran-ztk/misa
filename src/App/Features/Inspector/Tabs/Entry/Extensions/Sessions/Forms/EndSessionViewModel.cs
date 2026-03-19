using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class EndSessionViewModel(Guid itemId, InspectorGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    // Content
    [ObservableProperty] private string? _summary;

    [ObservableProperty] private SessionEfficiencyDto _selectedSessionEfficiency;
    [ObservableProperty] private SessionConcentrationDto _selectedSessionConcentration;

    public IReadOnlyList<SessionEfficiencyDto> Efficiencies { get; } = Enum.GetValues<SessionEfficiencyDto>();
    public IReadOnlyList<SessionConcentrationDto> Concentrations { get; } = Enum.GetValues<SessionConcentrationDto>();

    public string FormTitle { get; } = "End Session";
    public string? FormDescription { get; }

    public async Task<Result<Result>> SubmitAsync()
    {
        var dto = new StopSessionDto(
            itemId,
            SelectedSessionEfficiency,
            SelectedSessionConcentration,
            Summary
        );
        
        var result = await gateway.EndSessionAsync(dto);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}