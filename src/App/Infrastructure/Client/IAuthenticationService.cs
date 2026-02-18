using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Authentication;

namespace Misa.Ui.Avalonia.Infrastructure.Client;

public interface IAuthenticationService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto requestDto, CancellationToken ct = default);
    Task<AuthTokenResponseDto> LoginAsync(LoginRequestDto requestDto, CancellationToken ct = default);
}