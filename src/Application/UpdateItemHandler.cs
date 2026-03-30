namespace Misa.Application;

public record UpdateTitleRequest(Guid Id, string Title);
public record UpdateExpansionStateRequest(Guid Id, bool IsExpanded);
public record UpdateContentRequest(Guid ItemId, string Content);

public sealed class UpdateItemHandler(Repository repository)
{
    public async Task<bool> HandleAsync(UpdateTitleRequest updateTitleRequest)
    {
        var result = await repository.UpdateTitleAsync(updateTitleRequest.Id, updateTitleRequest.Title);
        return result;
    }
    public async Task HandleAsync(UpdateExpansionStateRequest r)
    {
        await repository.UpdateExpansionStateAsync(r.Id, r.IsExpanded);
    }
    public async Task HandleAsync(UpdateContentRequest r)
    {
        await repository.UpdateNoteContentAsync(r.ItemId, r.Content);
    }
}