using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Misa.Api.Middleware;

[Authorize]
public sealed class EventHub : Hub;