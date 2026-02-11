using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

public partial class EndSessionViewModel(Guid itemId) : ViewModelBase, IHostedForm<StopSessionDto>
{
    // Host
    public string Title => "End session";
    public string SubmitText => "Save";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    public StopSessionDto TrySubmit() => Submit();

    // Content
    [ObservableProperty] private string? _summary;

    [ObservableProperty] private EfficiencyContract _selectedEfficiency;
    [ObservableProperty] private ConcentrationContract _selectedConcentration;

    public IReadOnlyList<EfficiencyContract> Efficiencies { get; } = Enum.GetValues<EfficiencyContract>();
    public IReadOnlyList<ConcentrationContract> Concentrations { get; } = Enum.GetValues<ConcentrationContract>();

    private StopSessionDto Submit()
        => new(
            itemId,
            SelectedEfficiency,
            SelectedConcentration,
            Summary
        );
}