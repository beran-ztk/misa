using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Features.Sessions.Commands;

namespace Misa.Application.Items.Features.Sessions.Handlers;

public class StopExpiredSessionsHandler(IItemRepository repository)
{
    public async Task Handle(StopExpiredSessionsCommand command, CancellationToken ct)
    {
        
    }
}