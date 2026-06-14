using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.Views;

public partial class SettingsView : UserControl
{
    private static readonly string[] AudioExtensions = [".m4a", ".mp3", ".webm", ".opus"];
    private const string MusicDir = @"D:\media\music";

    public Action? PrepareForReset;
    public Action? OnResetComplete;

    public SettingsView()
    {
        InitializeComponent();
    }
}
