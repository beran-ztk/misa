using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed class ZettelkastenGateway(RemoteProxy remoteProxy)
{
    public async Task<Result> CreateTopicAsync(CreateTopicRequest requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ZettelkastenRoutes.CreateTopic)
        {
            Content = JsonContent.Create(requestBody)
        };

        return await remoteProxy.SendAsync(request);
    }

    public async Task<List<TopicListDto>?> GetTopicsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ZettelkastenRoutes.GetTopics);

        var response = await remoteProxy.SendAsync<List<TopicListDto>>(request);
        return response.Value
               ?? throw new Exception("No Data");
    }
}
