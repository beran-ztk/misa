using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.Views;

public partial class MainWindow : Window
{
    private const string MusicDir = @"D:\media\music";
    private static readonly string[] AudioExtensions = [".m4a", ".mp3", ".webm", ".opus"];

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(MusicDir);
        RefreshFileList();
    }

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(url)) return;

        DownloadBtn.IsEnabled = false;
        StatusText.Text = "Downloading…";

        var (success, errorOutput) = await RunYtDlp(url);

        StatusText.Text = success ? "Finished." : $"Failed:\n{errorOutput}";
        if (success) RefreshFileList();

        DownloadBtn.IsEnabled = true;
    }

    private static async Task<(bool success, string errorOutput)> RunYtDlp(string url)
    {
        var outputTemplate = Path.Combine(MusicDir, @"%(title)s [%(id)s].%(ext)s");

        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp.exe", //Path.Combine(toolsDir, "yt-dlp.exe")
            Arguments = $"--no-playlist -x --audio-format m4a -o \"{outputTemplate}\" \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stderr = await stderrTask;
        await stdoutTask;

        return (process.ExitCode == 0, stderr);
    }

    private void RefreshFileList()
    {
        var files = Directory.GetFiles(MusicDir)
            .Where(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .ToList();

        FileList.ItemsSource = files;
    }
}
