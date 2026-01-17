using System.Threading;
using System.Threading.Tasks;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public interface IClipboardService
{
    Task SetTextAsync(string? text);
}