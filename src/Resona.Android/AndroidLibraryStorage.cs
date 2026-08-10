using Android.App;
using Resona.Companion;

namespace Resona.Android;

public sealed class AndroidLibraryStorage : ILibraryStorage
{
    public string LibraryDirectory
    {
        get
        {
            var root = Application.Context.FilesDir?.AbsolutePath
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "MusicLibrary");
        }
    }
}
