using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Shell;

public partial class InfoBarViewModel(INavigationService navigationService) : ViewModelBase
{
    private readonly INavigationService _navigationService = navigationService;
}