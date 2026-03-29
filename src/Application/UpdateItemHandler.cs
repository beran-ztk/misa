namespace Misa.Application;

public record UpdateTitleRequest(Guid Id, string Title);

public sealed class UpdateItemHandler(Repository repository)
{
    public async Task<bool> HandleAsync(UpdateTitleRequest updateTitleRequest)
    {
        var result = await repository.UpdateTitleAsync(updateTitleRequest.Id, updateTitleRequest.Title);
        return result;
    }
}