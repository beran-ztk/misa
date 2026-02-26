using System.Threading.Tasks;
using Misa.Contract.Common.Results;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IHostedForm
{
    string Title { get; }
    string SubmitText { get; }
    string CancelText { get; }

    bool CanSubmit { get; }

    Task<Result> SubmitAsync();
}
public interface IHostedForm<TResult>
{
    string Title { get; }
    string SubmitText { get; }
    string CancelText { get; }

    bool CanSubmit { get; }

    Task<Result<TResult>> SubmitAsync();
}