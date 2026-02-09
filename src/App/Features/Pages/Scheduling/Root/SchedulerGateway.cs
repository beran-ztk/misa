using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed class SchedulerGateway(RemoteProxy remoteProxy, UserState userState)
{
    public async Task<List<ScheduleDto>> GetAllAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "scheduling");
        
        var response = await remoteProxy.SendAsync<List<ScheduleDto>?>(request);
        
        return response?.Value ?? [];
    }
    public async Task<ScheduleDto?> CreateAsync(AddScheduleDto dto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"scheduling/{userState.User.Id}");
        request.Content = JsonContent.Create(dto);
        
        var response = await remoteProxy.SendAsync<ScheduleDto?>(request);
            
        return response?.Value;
    }
}