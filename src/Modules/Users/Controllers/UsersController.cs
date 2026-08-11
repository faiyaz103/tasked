using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Users.Dtos;
using Users.Services;
namespace Users.Controllers;

[ApiController]
[Route("users")]
public class UsersController: ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserRequest> _validator;

    public UsersController(IUserService userService, IValidator<CreateUserRequest> validator)
    {
        _userService = userService;
        _validator = validator;
    }

    [HttpGet("hello")]
    public IActionResult GetHello()
    {
        var message = _userService.GetHelloMessage();
        return Ok(new {Message = message});
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // 1. Validate incoming request DTO
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            // 2. Execute business logic
            var result = await _userService.CreateUserAsync(request);

            // 3. Return 201 Created status with location header
            return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict if email is taken
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(user);
    }

}