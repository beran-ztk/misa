namespace Misa.Contract.Routes;

public static class TaskRoutes
{
    // Create
    public const string CreateTask = "items/task";
    
    // Get
    public const string GetTasks = "items/tasks";
    public const string GetTask = "items/{itemId:guid}/task";
    public static string GetTaskRequest(Guid itemId) => $"items/{itemId}/task";
    
    // Patch
    
    public const string UpdateTaskCategory = "items/{itemId:guid}/task/category";
    public static string UpdateTaskCategoryRequest(Guid itemId) => $"items/{itemId}/task/category";
    
    // Delete
    public const string DeleteTask = "items/{itemId:guid}/task";
    public static string DeleteTaskRequest(Guid itemId) => $"items/{itemId}/task/";
}