# Music

Music is a local desktop app for building, analyzing, and curating a personal music library.

It can import tracks from YouTube videos, playlists, mixes, and radio links, show a preview before queueing, and download the selected tracks in the background. After import, the app automatically runs audio analysis using multiple Essentia-based models to suggest genres, detect BPM, and measure loudness.

The app is built around everyday library work: listening, searching, filtering, rating tracks, reviewing model suggestions, adding genres and tags, marking songs for review, and keeping large imports manageable. It also includes local database backups and a portable Android library export.

## Linux

Linux x64 releases are distributed as an AppImage. Make the downloaded file executable and start it:

```bash
chmod +x Beran.Music.AppImage
./Beran.Music.AppImage
```

The .NET runtime is included. Audio playback currently uses the system `libvlc`; downloads and video export use `yt-dlp`, FFmpeg, FFprobe, and Node.js. On Arch Linux the runtime tools can be installed with:

```bash
sudo pacman -S vlc ffmpeg yt-dlp nodejs
```

Music first checks its bundled `Tools` directory and then the system `PATH`. Library paths are stored separately per operating system in `library-locations.json`, so Windows and Linux can point to the same tracks folder and SQLite database even when the shared drive has different mount paths.

The Runtime page in Settings shows whether libVLC and every command-line dependency are available. Linux desktop media controls use MPRIS, and the playback visualizer derives its audio levels through FFmpeg without taking audio-device ownership away from libVLC.

## Screenshots

![Library view](Docs/images/library.png)

![Track editor](Docs/images/edit.png)

![Channel-Manager](Docs/images/channels.png)

![Filter](Docs/images/filter.png)
