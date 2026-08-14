using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Users.Dtos;
using Users.Services;
namespace Users.Controllers;

[ApiController]
[Route("users")]
public class UsersController: ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<CreateProfileRequest> _profileReqValidator;
    private readonly IValidator<CreateUserRequest> _userReqValidator;
    private readonly IValidator<SignInUserRequest> _userSignInReqValidator;

    public UsersController(
        IUserService userService, 
        IValidator<CreateProfileRequest> profileValidator, 
        IValidator<CreateUserRequest> userReqValidator,
        IValidator<SignInUserRequest> userSignInReqValidator
    )
    {
        _userService = userService;
        _profileReqValidator = profileValidator;
        _userReqValidator = userReqValidator;
        _userSignInReqValidator = userSignInReqValidator;
    }

    [HttpGet("hello")]
    public IActionResult GetHello()
    {
        var message = _userService.GetHelloMessage();
        return Ok(new {Message = message});
    }

    // -------------------User------------------
    // create
    [HttpPost()]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // 1. Validate incoming request DTO
        var validationResult = await _userReqValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            // 2. Execute business logic
            var responseData = await _userService.CreateUserAsync(request);

            // 3. Return 201 Created status with location header
            return StatusCode(StatusCodes.Status201Created, responseData);
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict if email is taken
            return Conflict(new { message = ex.Message });
        }
    }

    // create
    [HttpPost("login")]
    public async Task<IActionResult> CreateUserSignIn([FromBody] SignInUserRequest request)
    {
        // 1. Validate incoming request DTO
        var validationResult = await _userSignInReqValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            // 2. Execute business logic
            var responseData = await _userService.CreateUserSignInAsync(request);

            // 3. Return 201 Created status with location header
            return StatusCode(StatusCodes.Status200OK, responseData);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Return 409 Conflict if email is taken
            return Unauthorized(new { message = ex.Message });
        }
    }

    // ---------------------Profile---------------------
    // create
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        // 1. Validate incoming request DTO
        var validationResult = await _profileReqValidator.ValidateAsync(request);
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