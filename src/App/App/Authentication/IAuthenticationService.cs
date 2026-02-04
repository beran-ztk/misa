using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Authentication;

namespace Misa.Ui.Avalonia.App.Authentication;

public interface IAuthenticationService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto requestDto, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto requestDto, CancellationToken ct = default);
}