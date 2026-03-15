using System;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Utilities.Toast;

public sealed partial class ToastViewModel : ViewModelBase
{
    private readonly Action _dismiss;

    public string  Title      { get; }
    public string? Message    { get; }
    public bool    HasMessage => !string.IsNullOrWhiteSpace(Message);

    public ToastViewModel(string title, string? message, Action dismiss)
    {
        Title    = title;
        Message  = message;
        _dismiss = dismiss;
    }

    [RelayCommand]
    public void Dismiss() => _dismiss();
}
