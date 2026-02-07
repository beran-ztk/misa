using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Authentication;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto requestDto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto requestDto, CancellationToken ct = default);
}