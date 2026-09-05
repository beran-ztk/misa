using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Resona.Models;

namespace Resona.Views;

public sealed class TrackVersionFields : StackPanel
{
    private sealed record OriginalChoice(int Id, string Title)
    {
        public override string ToString() => Title;
    }

    private readonly ComboBox _kind = new() { ItemsSource = new[] { "Original", "Edit / Remix" }, SelectedIndex = 0 };
    private readonly AutoCompleteBox _parent = new()
    {
        Watermark = "Search original track (optional)",
        MinimumPrefixLength = 0,
        FilterMode = AutoCompleteFilterMode.Contains
    };
    private readonly StackPanel _editFields = new() { Spacing = 7 };
    private readonly List<(TrackEditTypes Type, CheckBox Box)> _types = [];

    public bool IsOriginal => _kind.SelectedIndex == 0;
    public int? ParentTrackId => IsOriginal ? null : (_parent.SelectedItem as OriginalChoice)?.Id;
    public TrackEditTypes EditTypes => IsOriginal ? TrackEditTypes.None
        : _types.Where(item => item.Box.IsChecked == true).Aggregate(TrackEditTypes.None, (types, item) => types | item.Type);

    public TrackVersionFields()
    {
        Spacing = 8;
        Children.Add(_kind);
        Children.Add(_editFields);
        _editFields.Children.Add(new TextBlock { Text = "Edit type · select all that apply", FontSize = 11, Opacity = 0.7 });
        var typesPanel = new WrapPanel();
        foreach (var type in TrackVersions.Types)
        {
            var box = new CheckBox { Content = type.Name, Margin = new Thickness(0, 0, 14, 0), FontSize = 12 };
            _types.Add((type.Type, box));
            typesPanel.Children.Add(box);
        }
        _editFields.Children.Add(typesPanel);
        _editFields.Children.Add(new TextBlock { Text = "Original track", FontSize = 11, Opacity = 0.7 });
        _editFields.Children.Add(_parent);
        var detach = new Button { Content = "No parent", FontSize = 11, Padding = new Thickness(8, 4) };
        detach.Click += (_, _) => { _parent.SelectedItem = null; _parent.Text = string.Empty; };
        _editFields.Children.Add(detach);
        _kind.SelectionChanged += (_, _) => _editFields.IsVisible = !IsOriginal;
        _editFields.IsVisible = false;
    }

    public void Configure(IEnumerable<MusicTrack> tracks, bool isOriginal = true, int? parentId = null,
        TrackEditTypes types = TrackEditTypes.None, int? editingTrackId = null)
    {
        var originals = tracks.Where(track => track.IsOriginal && track.Id != editingTrackId)
            .OrderBy(track => track.DisplayTitle, StringComparer.OrdinalIgnoreCase)
            .Select(track => new OriginalChoice(track.Id, track.DisplayTitle)).ToList();
        _parent.ItemsSource = originals;
        _parent.SelectedItem = originals.FirstOrDefault(track => track.Id == parentId);
        _parent.Text = (_parent.SelectedItem as OriginalChoice)?.Title ?? string.Empty;
        _kind.SelectedIndex = isOriginal ? 0 : 1;
        foreach (var (type, box) in _types)
            box.IsChecked = types.HasFlag(type);
        _editFields.IsVisible = !IsOriginal;
    }

    public string? ValidationError => !IsOriginal && !string.IsNullOrWhiteSpace(_parent.Text) && ParentTrackId is null
        ? "Select an original from the search results, or choose No parent." : null;
}
