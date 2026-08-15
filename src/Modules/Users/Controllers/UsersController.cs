using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Shared.Infra.Auth;
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

    // -------------------User------------------
    // create
    [HttpPost()]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var validationResult = await _userReqValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var responseData = await _userService.CreateUserAsync(request);

            return StatusCode(StatusCodes.Status201Created, responseData);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // login
    [HttpPost("login")]
    public async Task<IActionResult> CreateUserSignIn([FromBody] SignInUserRequest request)
    {
        var validationResult = await _userSignInReqValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        try
        {
            var responseData = await _userService.CreateUserSignInAsync(request);

            return StatusCode(StatusCodes.Status200OK, responseData);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // sign out
    [Authorize]
    [HttpPost("signout")]
    public async Task<IActionResult> SignOutUser()
    {
        try
        {
            // 1. Extract the User ID from the Access Token claims
            Guid userId = User.ExtractUserId();
            await _userService.SignOutAsync(userId);
            return Ok(new { Message = "Successfully signed out." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // refresh tokens
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RotateTokenRequest request)
    {
        try
        {
            var tokens = await _userService.RotateTokensAsync(request);
            return Ok(tokens);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    // ---------------------Profile---------------------
    // create
    [Authorize]
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        // 1. Validate incoming request DTO
        var validationResult = await _profileReqValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        Guid userId = User.ExtractUserId();

        try
        {
            // 2. Execute business logic
            var responseData = await _userService.CreateProfileAsync(userId, request);

            // 3. Return 201 Created status with location header
            return StatusCode(StatusCodes.Status201Created, responseData);
        }
        catch (InvalidOperationException ex)
        {
            // Return 409 Conflict if email is taken
            return Conflict(new { message = ex.Message });
        }
    }

    // get
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        Guid userId = User.ExtractUserId();
        var user = await _userService.GetProfileAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Profile not found." });
        }

        return Ok(user);
    }

}