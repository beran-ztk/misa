namespace Misa.Contract.Items.Components.Activity;

public enum ActivityStateDto
{
    Draft,
    Undefined,

    InProgress,
    Active,
    Paused,
    Pending,

    WaitForResponse,

    Done,
    Canceled,
    Failed,
    Expired
}