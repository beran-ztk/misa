using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Utilities.Dev;

public sealed partial class DevToolsViewModel(DevGateway gateway) : ViewModelBase
{
    [ObservableProperty] private bool   _isBusy;
    [ObservableProperty] private string? _statusMessage;

    [RelayCommand]
    private async Task SeedDataAsync()
    {
        IsBusy        = true;
        StatusMessage = null;

        var result = await gateway.SeedDataAsync();

        StatusMessage = result.IsSuccess ? "Seed data generated." : "Failed to seed data.";
        IsBusy        = false;
    }

    [RelayCommand]
    private async Task DeleteDataAsync()
    {
        IsBusy        = true;
        StatusMessage = null;

        var result = await gateway.DeleteDataAsync();

        StatusMessage = result.IsSuccess ? "All data deleted." : "Failed to delete data.";
        IsBusy        = false;
    }
}
