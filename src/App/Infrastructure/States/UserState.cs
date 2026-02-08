using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Authentication;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public sealed partial class UserState : ObservableObject
{
    [ObservableProperty] private UserDto _user = new(Guid.Empty, string.Empty, string.Empty);
}