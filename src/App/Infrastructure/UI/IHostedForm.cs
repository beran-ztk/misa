using System.Threading.Tasks;
using Misa.Contract.Common.Results;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IHostedForm<TResult>
{
    Task<Result<TResult>> SubmitAsync();
}