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

    private async void OnAddTrackClicked(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;
        var downloaded = await new DownloadWindow().ShowDialog<bool>(owner);
        if (downloaded) RefreshTrackList();
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
        Dispatcher.UIThread.Post(() => NowPlayingText.Text = "");
    }

    private void StopPlayback()
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
    }
}
