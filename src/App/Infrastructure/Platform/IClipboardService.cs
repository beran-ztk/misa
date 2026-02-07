using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Misa.Ui.Avalonia.Infrastructure.Platform;

public interface IClipboardService
{
    Task SetTextAsync(string? text);
}
public sealed class ClipboardService : IClipboardService
{
    public async Task SetTextAsync(string? text)
    {
        text ??= string.Empty;

        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;

        if (mainWindow?.Clipboard is null)
            return;

        await mainWindow.Clipboard.SetTextAsync(text);
    }
}