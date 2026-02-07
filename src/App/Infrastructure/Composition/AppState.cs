using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Composition;

public class AppState(ShellState shellState)
{
    public required ShellState ShellState { get; init; } = shellState;
}