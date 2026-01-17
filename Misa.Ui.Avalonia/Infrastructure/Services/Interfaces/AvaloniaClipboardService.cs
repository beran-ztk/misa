using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public sealed class AvaloniaClipboardService : IClipboardService
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