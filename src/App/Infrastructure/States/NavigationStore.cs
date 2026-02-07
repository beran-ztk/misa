using System;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public partial class NavigationStore(HttpClient httpClient) : ObservableObject
{
    public HttpClient HttpClient { get; set; } = httpClient;
    [ObservableProperty] private UserDto _user = new(Guid.Empty, string.Empty, string.Empty);
}