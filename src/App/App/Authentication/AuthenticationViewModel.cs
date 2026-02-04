using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Authentication;

public partial class AuthenticationViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }

    public AuthenticationViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
    }
}