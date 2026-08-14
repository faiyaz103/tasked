using Tasks.Dtos;
using Tasks.Entities;

namespace Tasks.Services;

public interface ITaskService
{
    string GetHelloMessage();
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request);
}

public class TaskService: ITaskService
{   
    private readonly TasksDbContext _dbContext;

    public TaskService(TasksDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public string GetHelloMessage()
    {
        return "Hello from task module!";
    }

    // ---------------Tasks---------------
    // create tasks
    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request)
    {
        var task = new TaskGroup
        {
            Title = request.title
        };

        _dbContext.TaskGroups.Add(task);
        await _dbContext.SaveChangesAsync();

        var taskResponse = new TaskResponse(
            task.Title,
            task.CreatedAt
        );

        return taskResponse;
    }
}