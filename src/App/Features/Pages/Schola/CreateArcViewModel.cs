using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Schola;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Schola;

public sealed partial class CreateArcViewModel(ScholaGateway gateway) : ObservableObject, IHostedForm
{
    public string Title => "Create new Unit";
    public string SubmitText => "Create";
    public string CancelText => "Cancel";
    public bool CanSubmit => true;

    [ObservableProperty] private string _itemTitle = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _objective;
    [ObservableProperty] private ActivityPriorityDto _selectedPriority;
    public IReadOnlyList<ActivityPriorityDto> Priorities { get; } = Enum.GetValues<ActivityPriorityDto>();
    
    public async Task<Result> SubmitAsync()
    {
        var dto = new CreateArcRequest(
            ItemTitle, 
            Description, 
            SelectedPriority,
            Objective);
        return await gateway.CreateArcAsync(dto);
    }
}