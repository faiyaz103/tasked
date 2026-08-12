using Microsoft.AspNetCore.Mvc;
using Tasks.Services;

namespace Tasks.Controllers;

[ApiController]
[Route("tasks")]
public class TaskController: ControllerBase
{
    // Add interface to service and validator
    private readonly ITaskService _taskService;

    public TaskController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("hello")]
    public IActionResult GetHello()
    {
        var message = _taskService.GetHelloMessage();
        return Ok(new {Message = message});
    }
}