using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Users.Services;
namespace Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController: ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("hello")]
    public IActionResult GetHello()
    {
        var message = _userService.GetHelloMessage();
        return Ok(new {Message = message});
    }

}