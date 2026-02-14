using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Misa.Contract.Shared.Results;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public class RemoteProxy(HttpClient httpClient)
{
    public async Task<Result<T>?> SendAsync<T>(HttpRequestMessage requestMessage)
    {
        try
        {
            using var request = requestMessage;
            using var response = await httpClient.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result<T>>();
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        return null;
    }
    public async Task<Result?> SendAsync(HttpRequestMessage requestMessage)
    {
        try
        {
            using var request = requestMessage;
            using var response = await httpClient.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Result>();
            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}