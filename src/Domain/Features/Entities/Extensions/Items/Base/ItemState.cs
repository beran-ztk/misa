namespace Misa.Domain.Features.Entities.Extensions.Items.Base;

public enum ItemState
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
