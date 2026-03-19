using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class ScrollRevealHost : UserControl
{
    // ── RevealContent property ────────────────────────────────────────────────

    public static readonly StyledProperty<object?> RevealContentProperty =
        AvaloniaProperty.Register<ScrollRevealHost, object?>(nameof(RevealContent));

    public object? RevealContent
    {
        get => GetValue(RevealContentProperty);
        set => SetValue(RevealContentProperty, value);
    }

    // Push property changes to the inner ContentPresenter whenever they arrive.
    static ScrollRevealHost()
    {
        RevealContentProperty.Changed.AddClassHandler<ScrollRevealHost>((host, e) =>
        {
            if (host.PART_ContentPresenter is { } cp)
                cp.Content = e.NewValue;
        });
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public ScrollRevealHost()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Sync content in case it was set before the visual tree was ready.
        PART_ContentPresenter.Content = RevealContent;

        PART_Scroll.ScrollChanged += (_, _) => UpdateRevealLayer();
        PART_ScrollBtn.Click      += (_, _) => PART_Scroll.ScrollToEnd();

        UpdateRevealLayer();
    }

    // ── Reveal logic ──────────────────────────────────────────────────────────

    private void UpdateRevealLayer()
    {
        var extent   = PART_Scroll.Extent.Height;
        var viewport = PART_Scroll.Viewport.Height;
        var offset   = PART_Scroll.Offset.Y;

        // Hide the overlay when there is nothing below the current viewport.
        var atBottom = extent <= viewport + 2 || offset + viewport >= extent - 2;
        PART_RevealLayer.IsVisible = !atBottom;
    }
}
