namespace Misa.Contract.Shared.Results;

public static class ItemErrorCodes
{
    public const string ItemNotFound = "item.not_found";
    public const string ItemIdEmpty = "item_id.empty";
}
public static class DeadlineErrorCodes
{
    public const string DeadlineInPast = "deadline.in_past";
    public const string DeadlineNotFound = "deadline.not_found";
}