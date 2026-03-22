using System;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed class DeleteKnowledgeItemViewModel(Guid itemId, string title, ZettelkastenGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    public string FormTitle { get; } = "Delete item";
    public string? FormDescription { get; } = $"Delete \"{title}\"? This action cannot be undone.";

    public async Task<Result<Result>> SubmitAsync()
    {
        var result = await gateway.DeleteItemAsync(itemId);

        if (!result.IsSuccess)
            return Result<Result>.Failure();

        return Result<Result>.Ok(Result.Ok());
    }
}
