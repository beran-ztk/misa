using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Misa.Models;
using NAudio.Wave;

namespace Misa.Views;

public partial class MusicView : UserControl
{
    private const string MusicDir = @"D:\media\music";

    private IWavePlayer? _player;
    private WaveStream? _audioStream;
    private List<MusicTrack> _tracks = [];
    private int _playingTrackId = -1;

    public MusicView()
    {
        InitializeComponent();
        try
        {
            Db.Initialize();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Database error: {ex.Message}";
            return;
        }
        RefreshTrackList();
    }

    private void RefreshTrackList()
    {
        _tracks = Db.GetAllTracks();
        FileList.ItemsSource = _tracks.Select(t => t.Title).ToList();
    }

    public void Refresh()
    {
        NowPlayingText.Text = "";
        RefreshTrackList();
    }

    private async void OnAddTrackClicked(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;
        var downloaded = await new DownloadWindow().ShowDialog<bool>(owner);
        if (downloaded) RefreshTrackList();
    }

    private async void OnContextEditClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _tracks.Count) return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;

        var saved = await new EditTrackWindow(_tracks[idx]).ShowDialog<bool>(owner);
        if (saved) RefreshTrackList();
    }

    private async void OnContextDeleteClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _tracks.Count) return;

        var track = _tracks[idx];
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;

        var confirmed = await new ConfirmDialog(
            $"Delete \"{track.Title}\"?\n\nThis will remove the audio file and database entry.")
            .ShowDialog<bool>(owner);
        if (!confirmed) return;

        if (_playingTrackId == track.Id)
        {
            StopPlayback();
            NowPlayingText.Text = "";
        }

        Db.DeleteTrack(track.Id);

        try
        {
            var filePath = Path.Combine(MusicDir, track.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"File could not be deleted: {ex.Message}";
        }

        RefreshTrackList();
    }

    private void OnPlayClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _tracks.Count) return;

        //StopPlayback();

        try
        {
            _audioStream = new MediaFoundationReader(Path.Combine(MusicDir, _tracks[idx].FileName));
            _player = new WaveOutEvent();
            _player.PlaybackStopped += OnPlaybackStopped;
            _player.Init(_audioStream);
            _player.Play();
            _playingTrackId = _tracks[idx].Id;
            NowPlayingText.Text = _tracks[idx].Title;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Playback failed: {ex.Message}";
        }
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        StopPlayback();
        NowPlayingText.Text = "";
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            NowPlayingText.Text = "";
            _playingTrackId = -1;
        });
    }

    public void StopPlayback()
    {
        if (_player != null)
        {
            _player.PlaybackStopped -= OnPlaybackStopped;
            _player.Stop();
            _player.Dispose();
        }
        _audioStream?.Dispose();
        _player = null;
        _audioStream = null;
        _playingTrackId = -1;
    }
}
