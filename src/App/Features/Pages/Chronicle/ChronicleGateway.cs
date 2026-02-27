using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public class ChronicleGateway(RemoteProxy remoteProxy)
{
    public async Task<List<ChronicleEntryDto>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ChronicleRoutes.GetJournals);

        var response = await remoteProxy.SendAsync<List<ChronicleEntryDto>>(request);
        return response.Value
               ?? throw new Exception("No Data");
    }

    public async Task<Result> CreateAsync(CreateJournalRequest requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ChronicleRoutes.CreateJournal)
        {
            Content = JsonContent.Create(requestBody)
        };

        return await remoteProxy.SendAsync(request);
    }
}