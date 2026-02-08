using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Composition;

public class AppState(ShellState shellState, UserState userState)
{
    public required ShellState ShellState { get; init; } = shellState;
    public required UserState UserState { get; init; } = userState;
}