using System.Threading.Tasks;

namespace Misa.Ui.Avalonia.Infrastructure;

public interface IHostedForm<TResult>
{
    string FormTitle { get; }
    string? FormDescription { get; }
    Task<TResult> SubmitAsync();
}