using Avalonia.Controls;

namespace Music.Views;

public partial class MainWindow : Window
{
    private readonly MusicView _musicView = new();

    public MainWindow()
    {
        InitializeComponent();
        ContentArea.Content = _musicView;
    }
}
