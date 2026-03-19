using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Routes;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules;

public sealed class ScheduleGateway(RemoteProxy remoteProxy)
{
    public async Task<List<ScheduleDto>?> GetAllAsync()
    {
        var response = await remoteProxy.SendAsync<List<ScheduleDto>>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Get, ScheduleRoutes.GetSchedules),
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response.Value;
    }

    public async Task<Result<ScheduleDto>> CreateAsync(CreateScheduleRequest dto)
    {
        var response = await remoteProxy.SendAsync<ScheduleDto>(
            requestFactory: () => new HttpRequestMessage(HttpMethod.Post, ScheduleRoutes.CreateSchedule)
            {
                Content = JsonContent.Create(dto)
            },
            retry: new RetryOptions
            {
                MaxAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500)
            },
            cancellationToken: CancellationToken.None);

        return response;
    }

}