using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle.Root;

public sealed class ChronicleGateway(RemoteProxy remoteProxy, UserState userState)
{
    public async Task<Result<List<JournalEntryDto>>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "journals");

        return await remoteProxy.SendAsync<List<JournalEntryDto>>(request);
    }

    public async Task<Result<JournalEntryDto>> CreateAsync(CreateJournalEntryDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"journals/{userState.User.Id}")
        {
            Content = JsonContent.Create(dto)
        };

        return await remoteProxy.SendAsync<JournalEntryDto>(request);
    }
}