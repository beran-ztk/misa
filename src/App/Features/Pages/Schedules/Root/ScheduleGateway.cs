using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules.Root;

public sealed class ScheduleGateway(RemoteProxy remoteProxy)
{
    public async Task<List<ScheduleDto>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ScheduleRoutes.GetSchedules);

        var response = await remoteProxy.SendAsync<List<ScheduleDto>>(request);
        return response.Value ?? throw new NoNullAllowedException();
    }

    public async Task<Result<ScheduleDto>> CreateAsync(CreateScheduleRequest dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ScheduleRoutes.CreateSchedule)
        {
            Content = JsonContent.Create(dto)
        };

        var response = await remoteProxy.SendAsync<ScheduleDto>(request);
        return response ?? throw new NoNullAllowedException();
    }

}