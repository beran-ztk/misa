using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Music.Companion;

namespace Music.Android;

[Activity(
    Label = "Music",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        CompanionServices.AudioPlayer = new AndroidAudioPlayer();
        CompanionServices.LibraryStorage = new AndroidLibraryStorage();
        CompanionServices.MediaControls = new AndroidMediaControls(this);

        // Keep Android's system font fallback enabled so emoji and uncommon
        // Unicode symbols can be resolved by the platform font collection.
        return base.CustomizeAppBuilder(builder);
    }
}
