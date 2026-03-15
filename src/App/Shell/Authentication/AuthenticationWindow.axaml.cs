using System;
using Avalonia.Controls;

namespace Misa.Ui.Avalonia.Shell.Authentication;

public partial class AuthenticationWindow : Window
{
    public AuthenticationWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UsernameField.Focus();
    }
}