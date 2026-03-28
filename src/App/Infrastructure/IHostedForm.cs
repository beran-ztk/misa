using System.Threading.Tasks;
using Misa.Contract.Common.Results;

namespace Misa.Ui.Avalonia.Infrastructure;

public interface IHostedForm<TResult>
{
    string FormTitle { get; }
    string? FormDescription { get; }
    Task<Result<TResult>> SubmitAsync();
}