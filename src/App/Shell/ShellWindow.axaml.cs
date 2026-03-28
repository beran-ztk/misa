using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Misa.Ui.Avalonia.Shell;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.L && e.KeyModifiers == KeyModifiers.Control)
        {
            var searchBox = this.GetVisualDescendants()
                .OfType<TextBox>()
                .FirstOrDefault(t => t.Classes.Contains("workspace-toolbar-search")
                                     && t.IsEffectivelyVisible
                                     && t.IsEffectivelyEnabled);

            if (searchBox is not null)
            {
                searchBox.Focus();
                e.Handled = true;
            }
        }
    }
}