using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Composition;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.Time;
using Misa.Ui.Avalonia.Shell.Base;

namespace Misa.Ui.Avalonia.Shell.Authentication;

public partial class AuthenticationWindowViewModel : ViewModelBase
{
    private IAuthenticationService AuthService { get; }
    private TimeZoneService TimeZoneService { get; }
    private UserState UserState { get; }
    private IServiceProvider Services { get; }
    private RemoteProxy RemoteProxy { get; }
    public AuthenticationWindowViewModel(
        IAuthenticationService authService,
        TimeZoneService timeZoneService,
        UserState userState,
        IServiceProvider services,
        RemoteProxy remoteProxy)
    {
        AuthService = authService;
        TimeZoneService = timeZoneService;
        UserState = userState;
        Services = services;
        RemoteProxy = remoteProxy;

        TimeZoneIds = new ObservableCollection<string>(TimeZoneService.Ids);
        SelectedTimeZoneId = TimeZoneIds.Contains("Europe/Berlin") ? "Europe/Berlin" : TimeZoneIds.FirstOrDefault();

        IsRegisterMode = false;
    }

    public ObservableCollection<string> TimeZoneIds { get; }
    [ObservableProperty] private string? _selectedTimeZoneId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubtitleText))]
    private bool _isRegisterMode;

    public string SubtitleText => IsRegisterMode ? "Create your account" : "Sign in to continue";
    public bool IsLoginMode => !IsRegisterMode;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    [ObservableProperty] private bool _isBusy;
    public bool IsNotBusy => !IsBusy;

    [ObservableProperty] private string? _errorMessage;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string PrimaryButtonText => IsRegisterMode ? "Register" : "Sign in";

    public bool CanSubmit
    {
        get
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(Username)) return false;
            if (string.IsNullOrWhiteSpace(Password)) return false;

            if (IsRegisterMode)
            {
                if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal)) return false;
                if (string.IsNullOrWhiteSpace(SelectedTimeZoneId)) return false;
            }

            return true;
        }
    }

    partial void OnIsRegisterModeChanged(bool value)
    {
        ErrorMessage = null;
        ConfirmPassword = string.Empty;

        if (value && string.IsNullOrWhiteSpace(SelectedTimeZoneId))
            SelectedTimeZoneId = TimeZoneIds.Contains("Europe/Berlin") ? "Europe/Berlin" : TimeZoneIds.FirstOrDefault();

        OnPropertyChanged(nameof(IsLoginMode));
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnConfirmPasswordChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnSelectedTimeZoneIdChanged(string? value) => OnPropertyChanged(nameof(CanSubmit));

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    [RelayCommand]
    private void SetLoginMode() => IsRegisterMode = false;

    [RelayCommand]
    private void SetRegisterMode() => IsRegisterMode = true;

    [RelayCommand]
    private void Bypass()
    {
        // Optional: Wenn du Bypass behalten willst, hier entweder Dummy-User setzen oder einfach Shell öffnen.
        OpenShellAndCloseAuth();
    }

    [RelayCommand]
    private void Close()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (!CanSubmit) return;

        ErrorMessage = null;
        IsBusy = true;

        try
        {
            AuthResponseDto responseDto;

            if (IsRegisterMode)
            {
                var req = new RegisterRequestDto(
                    "beran@misa.com",
                    Username.Trim(),
                    Password,
                    SelectedTimeZoneId ?? "Europe/Berlin");

                responseDto = await AuthService.RegisterAsync(req);
            }
            else
            {
                var req = new LoginRequestDto(
                    Username.Trim(),
                    Password);

                responseDto = await AuthService.LoginAsync(req);
            }

            UserState.User = responseDto.User;

            OpenShellAndCloseAuth();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenShellAndCloseAuth()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var authWindow = desktop.MainWindow;

        var shellWindow = Services.GetRequiredService<ShellWindow>();
        desktop.MainWindow = shellWindow;
        shellWindow.Show();

        authWindow?.Close();
    }
}
