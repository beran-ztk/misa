namespace Misa.Contract.Items.Components.Activity;

public record ChangeActivityStateRequest(
    ActivityStateDto State,
    string? Reason = null);
