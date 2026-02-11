namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public enum ItemStateDto
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