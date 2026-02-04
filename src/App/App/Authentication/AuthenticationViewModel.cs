using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Authentication;

public partial class AuthenticationViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }
    private IAuthenticationService AuthService { get; }

    public AuthenticationViewModel(
        INavigationService navigationService,
        IAuthenticationService authService)
    {
        NavigationService = navigationService;
        AuthService = authService;

        IsRegisterMode = false;
    }

    [ObservableProperty] private bool _isRegisterMode;

    public bool IsLoginMode => !IsRegisterMode;

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    [ObservableProperty] private bool _isBusy;
    public bool IsNotBusy => !IsBusy;

    [ObservableProperty] private string? _errorMessage;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string PrimaryButtonText => IsRegisterMode ? "Registrieren" : "Anmelden";

    public bool CanSubmit
    {
        get
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(Username)) return false;
            if (string.IsNullOrWhiteSpace(Password)) return false;

            if (IsRegisterMode)
            {
                if (Password.Length < 1) return false;
                if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal)) return false;
            }

            return true;
        }
    }

    partial void OnIsRegisterModeChanged(bool value)
    {
        ErrorMessage = null;
        ConfirmPassword = string.Empty;
        OnPropertyChanged(nameof(IsLoginMode));
        OnPropertyChanged(nameof(PrimaryButtonText));
        OnPropertyChanged(nameof(CanSubmit));
    }

    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnConfirmPasswordChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(CanSubmit));
    }
    partial void OnErrorMessageChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand]
    private void SetLoginMode() => IsRegisterMode = false;

    [RelayCommand]
    private void SetRegisterMode() => IsRegisterMode = true;

    [RelayCommand]
    private void Close()
    {
        NavigationService.NavigationStore.CloseOverlay();
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
                    Username.Trim(),
                    Password,
                    "Europe/Berlin");

                responseDto = await AuthService.RegisterAsync(req);
            }
            else
            {
                var req = new LoginRequestDto(
                    Username.Trim(),
                    Password);

                responseDto = await AuthService.LoginAsync(req);
            }

            NavigationService.NavigationStore.User = responseDto.User;
            
            NavigationService.NavigationStore.CloseOverlay();
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
}
