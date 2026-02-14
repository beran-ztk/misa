using System.Threading.Tasks;
using Misa.Contract.Shared.Results;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IHostedForm<TResult>
{
    string Title { get; }
    string SubmitText { get; }
    string CancelText { get; }

    bool CanSubmit { get; }

    Task<Result<TResult>> SubmitAsync();
}