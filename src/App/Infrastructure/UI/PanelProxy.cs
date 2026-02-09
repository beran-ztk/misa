using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public class PanelProxy(ShellState shellState, IPanelFactory panelFactory)
{
    public void Close() => shellState.Panel = null;
    public void Open(PanelKey key) => shellState.Panel = panelFactory.Create(key);
}