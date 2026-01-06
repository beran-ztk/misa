using System;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public interface IEntityDetailHost
{
    Guid ActiveEntityId { get; set; }
    INavigationService NavigationService { get; }
}