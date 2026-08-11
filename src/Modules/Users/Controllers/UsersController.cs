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
    private readonly IValidator<CreateProfileRequest> _validator;

    public UsersController(IUserService userService, IValidator<CreateProfileRequest> validator)
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

    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
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
            var (id, responseData) = await _userService.CreateProfileAsync(request);

            // 3. Return 201 Created status with location header
            return CreatedAtAction(nameof(GetProfileById), new { id }, responseData);
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict if email is taken
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("profile/{id:guid}")]
    public async Task<IActionResult> GetProfileById(Guid id)
    {
        var user = await _userService.GetProfileAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Profile not found." });
        }

        return Ok(user);
    }

}