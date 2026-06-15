namespace Misa.Music.Models;

public class MusicSettings
{
    public string MusicDirectory { get; set; } = @"D:\media\music";
    public string ToolsDirectory { get; set; } = string.Empty;
    public bool UseNodeJsRuntime { get; set; } = true;
    public bool UseFirefoxCookies { get; set; } = false;
    public bool UseRemoteEjsComponents { get; set; } = false;

    // Player settings
    public int Volume { get; set; } = 100;
    public bool IsMuted { get; set; } = false;
    public bool ShuffleEnabled { get; set; } = false;
    public bool AutoplayEnabled { get; set; } = false;
    public string RepeatMode { get; set; } = "None"; // "None" | "RepeatOne" | "RepeatAll"
    public bool CrossfadeEnabled { get; set; } = false;
    public int CrossfadeDurationSeconds { get; set; } = 10;
    public int ManualSwitchFadeDurationSeconds { get; set; } = 2;
    public bool ShowUpcomingTrackBar { get; set; } = true;
}
