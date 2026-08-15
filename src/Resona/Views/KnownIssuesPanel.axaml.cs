using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Resona.Services;

namespace Resona.Views;

public partial class KnownIssuesPanel : UserControl
{
    private GitHubIssuesFetchResult _current = new([], null, IsCached: true);
    private bool _loading;

    public KnownIssuesPanel()
    {
        InitializeComponent();
        ApplyResult(GitHubIssuesService.Current.LoadCached());
    }

    public event Action? CloseRequested;
    public event Action<GitHubIssuesFetchResult>? IssuesChanged;
    public GitHubIssuesFetchResult CurrentIssues => _current;

    public void Open()
    {
        IsVisible = true;
        ApplyResult(_current);
    }

    public async void LoadAtStartup() => await RefreshAsync(showLoading: false);

    private async void OnRefreshClicked(object? sender, RoutedEventArgs e) => await RefreshAsync(showLoading: true);

    private async System.Threading.Tasks.Task RefreshAsync(bool showLoading)
    {
        if (_loading)
            return;

        _loading = true;
        if (showLoading)
            UpdatedText.Text = "Refreshing GitHub issues…";
        try
        {
            ApplyResult(await GitHubIssuesService.Current.RefreshAsync());
        }
        finally
        {
            _loading = false;
        }
    }

    private void ApplyResult(GitHubIssuesFetchResult result)
    {
        _current = result;
        UpdatedText.Text = result.FetchedAt is DateTimeOffset fetchedAt
            ? result.IsCached
                ? $"Last saved {fetchedAt.ToLocalTime():dd.MM.yyyy HH:mm}"
                : $"Updated {fetchedAt.ToLocalTime():dd.MM.yyyy HH:mm}"
            : result.Error is null ? "Not loaded yet" : "GitHub unavailable";
        SummaryText.Text = result.Error ?? (result.Issues.Count == 1 ? "1 open issue from GitHub" : $"{result.Issues.Count} open issues from GitHub");

        IssueRows.Children.Clear();
        foreach (var issue in result.Issues)
            IssueRows.Children.Add(CreateIssueRow(issue));

        EmptyPanel.IsVisible = result.Issues.Count == 0;
        EmptyTitleText.Text = result.Error is null ? "No open issues" : "Issues unavailable";
        EmptyDescriptionText.Text = result.Error ?? "There are currently no open issues on GitHub.";
        IssuesChanged?.Invoke(result);
    }

    private static Control CreateIssueRow(GitHubIssue issue)
    {
        var row = new Border
        {
            Classes = { "issue-row" },
            Background = new SolidColorBrush(Color.Parse("#08FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#18FFFFFF")),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Avalonia.Thickness(10, 8),
            Cursor = new Cursor(StandardCursorType.Hand),
            DataContext = issue
        };
        var content = new StackPanel { Spacing = 4 };
        var headline = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 9 };
        headline.Children.Add(new TextBlock
        {
            Text = $"#{issue.Number}",
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#92BDE8")),
            VerticalAlignment = VerticalAlignment.Top
        });
        var title = new TextBlock
        {
            Text = issue.Title,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(title, 1);
        headline.Children.Add(title);
        content.Children.Add(headline);

        if (issue.Labels.Count > 0)
            content.Children.Add(new TextBlock
            {
                Text = string.Join(" · ", issue.Labels),
                FontSize = 9.5,
                Opacity = 0.52,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

        row.Child = content;
        row.Tapped += OnIssueTapped;
        return row;
    }

    private static void OnIssueTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Control { DataContext: GitHubIssue issue }
            || !Uri.TryCreate(issue.Url, UriKind.Absolute, out var uri))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // The local shell can be unavailable in restricted desktop sessions.
        }
        e.Handled = true;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        IsVisible = false;
        CloseRequested?.Invoke();
    }
}
