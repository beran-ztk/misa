using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Authentication;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public sealed partial class UserState : ObservableObject
{
    [ObservableProperty] private Guid _id = Guid.Empty;
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _token = string.Empty;
}