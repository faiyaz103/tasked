### Repository Structure

```
MyModularBackend/
├── MyModularBackend.sln                       # Solution file grouping all projects
├── src/
│   ├── Host/
│   │   └── WebApi/                            # App entrypoint (wires up all modules)
│   │       ├── Controllers/                   # (Optional) Global/health check controllers
│   │       ├── Program.cs                     # Startup config, middleware, CORS, DI wiring
│   │       ├── appsettings.json               # Config (DB Connection strings, API keys)
│   │       └── WebApi.csproj
│   │
│   ├── Shared/
│   │   └── Shared.Infrastructure/             # Cross-cutting code used by multiple modules
│   │       ├── Validation/                    # Custom validation filters/behaviors
│   │       ├── Database/                      # Global EF Core helpers/interceptor
│   │       └── Shared.Infrastructure.csproj
│   │
│   └── Modules/
│       └── Users/                             # Isolated User Module
│           ├── Controllers/                   # REST API Endpoints (e.g., UserController.cs)
│           ├── Services/                      # Business Logic (e.g., UserService.cs)
│           ├── Dtos/                          # Request/Response contracts & FluentValidation
│           ├── Entities/                      # PostgreSQL Db context & database models
│           ├── UsersModule.cs                 # Extension method registering Users services in DI
│           └── Users.csproj
```
---
### Step 1: Initialize Solution and Projects via .NET CLI
```
# Create solution directory & solution file
mkdir MyModularBackend
cd MyModularBackend
dotnet new sln -n MyModularBackend

# Create projects
dotnet new webapi -o src/Host/WebApi --no-openapi
dotnet new classlib -o src/Shared/Shared.Infrastructure
dotnet new classlib -o src/Modules/Users

# Add projects to solution
dotnet sln add src/Host/WebApi/WebApi.csproj
dotnet sln add src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj
dotnet sln add src/Modules/Users/Users.csproj

# Set up project references (Host depends on Modules & Shared)
dotnet add src/Host/WebApi/WebApi.csproj reference src/Modules/Users/Users.csproj
dotnet add src/Host/WebApi/WebApi.csproj reference src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj
dotnet add src/Modules/Users/Users.csproj reference src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj
```
---
### Step 2: Install NuGet Packages
We will install FluentValidation (the standard validation library in .NET, matching NestJS DTO validation) and EF Core PostgreSQL packages.
```
# In Modules/Users:
dotnet add src/Modules/Users/Users.csproj package FluentValidation.DependencyInjectionExtensions
dotnet add src/Modules/Users/Users.csproj package Npgsql.EntityFrameworkCore.PostgreSQL

# In Host/WebApi:
dotnet add src/Host/WebApi/WebApi.csproj package Microsoft.AspNetCore.OpenApi
```
---
### Step 3: Create Service Layer
```csharp
namespace Users.Services;

public interface IUserService
{
    string GetHelloMessage();
}

public class UserService : IUserService
{
    public string GetHelloMessage()
    {
        return "Hello World from Users Module!";
    }
}
```
---
### Step 4: Create Controller
```csharp
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Users.Dtos;
using Users.Services;

namespace Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
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
        return Ok(new { Message = message });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Request validation similar to NestJS ValidationPipe
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        return Ok(new { Message = $"User {request.Name} created successfully!" });
    }
}
```
---
### Step 5: Create Module Registration Extension
```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Users.Dtos;
using Users.Services;

namespace Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        // Register Services
        services.AddScoped<IUserService, UserService>();

        // Register Validators in this assembly
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        return services;
    }
}
```
---
### Step 6: Wire Everything in Host (Program.cs)
```csharp
using Users;

var builder = WebApplication.CreateBuilder(args);

// add controllers
builder.Services.AddControllers();

// register modules
builder.Services.AddUserModule();

// configure CORS (prepare for FE later)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

// map controllers
app.MapControllers();

app.Run();

```
---
### Verify & Test the Endpoints
```
dotnet run --project src/Host/WebApi/WebApi.csproj
```