using System;
using System.IO;
using System.Text.Json;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

internal sealed class ZettelkastenSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Misa", "zettelkasten.json");

    public Guid? LastSelectedItemId { get; set; }

    public static ZettelkastenSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ZettelkastenSettings>(json) ?? new();
        }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch { }
    }
}
