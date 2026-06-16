using System;
using System.IO;
using Misa.Models;

namespace Misa.Services;

public class TrackFileService
{
    private readonly string _musicDir;

    public TrackFileService(string musicDir)
    {
        _musicDir = musicDir;
    }

    public string GetFilePath(string fileName) => Path.Combine(_musicDir, fileName);

    public DeleteTrackResult TryDeleteFile(string fileName)
    {
        try
        {
            var filePath = GetFilePath(fileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
            return new DeleteTrackResult(true);
        }
        catch (Exception ex)
        {
            return new DeleteTrackResult(false, ex.Message);
        }
    }
}
