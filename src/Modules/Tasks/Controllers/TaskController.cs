using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Tasks.Dtos;
using Tasks.Services;

namespace Tasks.Controllers;

[ApiController]
[Route("tasks")]
public class TaskController: ControllerBase
{
    // Add interface to service and validator
    private readonly ITaskService _taskService;
    private readonly IValidator<CreateTaskRequest> _validator;

    public TaskController(ITaskService taskService, IValidator<CreateTaskRequest> validator)
    {
        _taskService = taskService;
        _validator = validator;
    }

    [HttpGet("hello")]
    public IActionResult GetHello()
    {
        var message = _taskService.GetHelloMessage();
        return Ok(new {Message = message});
    }

    [HttpPost()]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var responseData = await _taskService.CreateTaskAsync(request);
            return StatusCode(StatusCodes.Status201Created, responseData);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });

        }
    }
}