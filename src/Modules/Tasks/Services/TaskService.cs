namespace Tasks.Services;

public interface ITaskService
{
    string GetHelloMessage();
}

public class TaskService: ITaskService
{
    public string GetHelloMessage()
    {
        return "Hello from task module!";
    }
}