namespace Misa.Contract.Routes;

public static class TaskRoutes
{
    public const string Create = "items/task";
    public const string GetAll = "items/tasks";
    public const string UpdateCategory = "items/{itemId:guid}/task/category";
    public static string UpdateCategoryRequest(Guid itemId) => $"items/{itemId}/task/category";
    
    public const string Delete = "items/{itemId:guid}/task";
    public static string DeleteRequest(Guid itemId) => $"items/{itemId}/task/";
}