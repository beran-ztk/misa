using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class EndSessionViewModel(Guid itemId, InspectorGateway gateway) : ViewModelBase, IHostedForm
{
    // Host
    public string Title => "End session";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;
    // Content
    [ObservableProperty] private string? _summary;

    [ObservableProperty] private EfficiencyContract _selectedEfficiency;
    [ObservableProperty] private ConcentrationContract _selectedConcentration;

    public IReadOnlyList<EfficiencyContract> Efficiencies { get; } = Enum.GetValues<EfficiencyContract>();
    public IReadOnlyList<ConcentrationContract> Concentrations { get; } = Enum.GetValues<ConcentrationContract>();

    public async Task<Result> SubmitAsync()
    {

        var dto = new StopSessionDto(
            itemId,
            SelectedEfficiency,
            SelectedConcentration,
            Summary
        );
        
        return await gateway.EndSessionAsync(dto);
    }
}