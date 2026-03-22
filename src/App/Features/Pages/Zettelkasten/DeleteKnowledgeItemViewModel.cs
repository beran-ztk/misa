using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed class DeleteKnowledgeItemViewModel(
    Guid[] ids,
    ZettelkastenGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    public string FormTitle { get; } = "Delete item";
    public string? FormDescription { get; } = $"Deleting {ids.Length} items.";

    public async Task<Result<Result>> SubmitAsync()
    {
        var result = await gateway.DeleteSubtreeAsync(ids);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}
