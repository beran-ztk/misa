namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IHostedForm<out TResult>
{
    string Title { get; }
    string SubmitText { get; }
    string CancelText { get; }
    bool CanSubmit { get; }

    TResult? TrySubmit();
}