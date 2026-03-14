using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Shell.Authentication;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class HeaderViewModel : ViewModelBase
{
    public UserState UserState { get; }
    private IServiceProvider Services { get; }

    public HeaderViewModel(UserState userState, IServiceProvider services)
    {
        UserState = userState;
        Services = services;
    }

    [RelayCommand]
    private void Logout()
    {
        UserState.Id = Guid.Empty;
        UserState.Username = string.Empty;
        UserState.Token = string.Empty;

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var shellWindow = desktop.MainWindow;
        var authWindow = Services.GetRequiredService<AuthenticationWindow>();

        if (authWindow.DataContext is AuthenticationWindowViewModel authVm)
            authVm.Reset();

        desktop.MainWindow = authWindow;
        authWindow.Show();
        shellWindow?.Close();
    }
}
