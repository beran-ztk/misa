using System.Threading.Tasks;

namespace Misa.Ui.Avalonia.Features.Pages.Common;

public interface IItemFacade
{
    Task InitializeWorkspaceAsync();
    Task RefreshWorkspaceAsync();

    void ShowAddPanel();
    void ClosePanel();

    Task SubmitCreateAsync();
}
