namespace Misa.Music.Models;

public class MusicSettings
{
    public string MusicDirectory { get; set; } = @"D:\media\music";
    public string ToolsDirectory { get; set; } = string.Empty;
    public bool UseNodeJsRuntime { get; set; } = true;
    public bool UseFirefoxCookies { get; set; } = false;
    public bool UseRemoteEjsComponents { get; set; } = false;
}
