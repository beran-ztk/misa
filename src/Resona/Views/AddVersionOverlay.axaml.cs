using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Resona.Models;
using Resona.Services;

namespace Resona.Views;

public partial class AddVersionOverlay : UserControl
{
    private sealed record TypeChoice(TrackEditTypes Type, string Name)
    {
        public override string ToString() => Name;
    }

    private MusicTrack? _original;
    private bool _submitting;
    public event Action? Queued;

    public AddVersionOverlay()
    {
        InitializeComponent();
        TypeBox.ItemsSource = TrackVersions.Types.Select(type => new TypeChoice(type.Type, type.Name))
            .Append(new TypeChoice(TrackEditTypes.Slowed | TrackEditTypes.Reverb, "Slowed + Reverb"))
            .ToList();
    }

    public void Open(MusicTrack original)
    {
        _original = original;
        _submitting = false;
        TypeBox.SelectedIndex = -1;
        UrlBox.Text = string.Empty;
        OriginalTitleText.Text = original.DisplayTitle;
        ToolTip.SetTip(OriginalTitleText, original.DisplayTitle);
        ErrorText.IsVisible = false;
        ShowStep(urlStep: false);
        IsVisible = true;
        Dispatcher.UIThread.Post(() => TypeBox.Focus());
    }

    private void ShowStep(bool urlStep)
    {
        TypeStep.IsVisible = !urlStep;
        UrlStep.IsVisible = urlStep;
        DialogCard.Width = urlStep ? 500 : 380;
        if (urlStep && TypeBox.SelectedItem is TypeChoice choice)
        {
            SelectedTypeText.Text = choice.Name;
            Dispatcher.UIThread.Post(() => UrlBox.Focus());
        }
    }

    private void OnTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NextButton is not null)
            NextButton.IsEnabled = TypeBox.SelectedItem is TypeChoice;
    }

    private void OnUrlChanged(object? sender, TextChangedEventArgs e)
    {
        if (QueueButton is null) return;
        QueueButton.IsEnabled = !string.IsNullOrWhiteSpace(UrlBox.Text);
        ErrorText.IsVisible = false;
    }

    private void OnNextClicked(object? sender, RoutedEventArgs e)
    {
        if (TypeBox.SelectedItem is TypeChoice) ShowStep(urlStep: true);
    }

    private void OnBackClicked(object? sender, RoutedEventArgs e) => ShowStep(urlStep: false);
    private void OnCloseClicked(object? sender, RoutedEventArgs e) => IsVisible = false;

    private void OnQueueClicked(object? sender, RoutedEventArgs e)
    {
        if (_submitting || _original is null || TypeBox.SelectedItem is not TypeChoice choice) return;
        _submitting = true;
        try
        {
            ImportQueueService.Current.QueueVersion(_original, choice.Type, UrlBox.Text ?? string.Empty);
        }
        catch (Exception exception)
        {
            _submitting = false;
            ErrorText.Text = exception.Message;
            ErrorText.IsVisible = true;
            return;
        }
        IsVisible = false;
        Queued?.Invoke();
    }

    private void OnDialogKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            IsVisible = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && UrlStep.IsVisible && QueueButton.IsEnabled)
        {
            OnQueueClicked(sender, e);
            e.Handled = true;
        }
    }
}
