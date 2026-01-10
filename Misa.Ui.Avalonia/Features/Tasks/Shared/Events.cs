using System;
using Misa.Contract.Items;
using Misa.Contract.Items.Common;

namespace Misa.Ui.Avalonia.Features.Tasks.Shared;

public interface IEventBus
{
    IDisposable Subscribe<T>(Action<T> handler);
    void Publish<T>(T evt);
}

// Events
public sealed record OpenCreateRequested;
public sealed record CloseRightPaneRequested;
public sealed record ReloadTasksRequested;

public sealed record TaskCreated(ReadItemDto Created);
public sealed record TaskCreateFailed(string Message);