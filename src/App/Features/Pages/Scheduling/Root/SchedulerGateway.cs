using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed class SchedulerGateway(RemoteProxy remoteProxy, UserState userState)
{
    public async Task<Result<IReadOnlyCollection<ScheduleExtensionDto>>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "scheduling");

        return await remoteProxy.SendAsync<IReadOnlyCollection<ScheduleExtensionDto>>(request);
    }

    public async Task<Result<ScheduleExtensionDto>> CreateAsync(AddScheduleDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"scheduling/{userState.Id}")
        {
            Content = JsonContent.Create(dto)
        };

        return await remoteProxy.SendAsync<ScheduleExtensionDto>(request);
    }

}