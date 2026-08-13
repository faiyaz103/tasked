Implementing a complete JWT authentication and authorization pipeline with Refresh Token Rotation and RBAC requires coordinating a few components: password hashing, token generation, database updates, and endpoint protection.

Here is the step-by-step guide with functional code for your **Modular Monolith**.

### Step 1: Install Required Packages

Run this in your terminal at the solution root:

```bash
dotnet add src/Host/WebApi/WebApi.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Modules/Users/Users.csproj package BCrypt.Net-Next

```

*(We use `BCrypt.Net-Next` as the industry standard for hashing passwords and refresh tokens).*

---

### Step 2: Update the `Profile` Entity

We need to add authentication fields to the `Users` module's entity.

**`src/Modules/Users/Entities/Profile.cs`**

```csharp
using Shared.Infra.Enums;

namespace Users.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty; 
    public string Role { get; set; } = "User"; // RBAC (e.g., "User", "Admin")
    
    // Refresh Token tracking
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public string Phone { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Other;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

```

*(Don't forget to run `dotnet ef migrations add AddAuthFields` and `database update` after this!)*

---

### Step 3: Create the Token Service

Place this in your `Shared.Infra` project so it can be registered centrally.

**`src/Shared/Shared.Infra/Auth/TokenService.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Infra.Auth;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        var secret = _configuration["JwtSettings:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:AccessTokenExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!)),
            ValidateLifetime = false // Ignore expiration here!
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}

```

---

### Step 4: Core Auth Service (Signup, Login, Refresh)

Place this inside the `Users` module. It uses `BCrypt` to safely hash both the password and the refresh token.

**`src/Modules/Users/Services/AuthService.cs`**

```csharp
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Shared.Infra.Auth;
using Users.Entities;

namespace Users.Services;

public record AuthResponse(string AccessToken, string RefreshToken);

public class AuthService
{
    private readonly UsersDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public AuthService(UsersDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> SignUpAsync(string email, string password, string role, string firstName, string lastName)
    {
        if (await _dbContext.Profiles.AnyAsync(p => p.Email == email))
            throw new Exception("Email already exists");

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        var tokens = await UpdateTokensAsync(profile);
        _dbContext.Profiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        return tokens;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.Email == email) 
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var tokens = await UpdateTokensAsync(profile);
        await _dbContext.SaveChangesAsync();

        return tokens;
    }

    public async Task<AuthResponse> RefreshTokenAsync(string expiredAccessToken, string refreshToken)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(expiredAccessToken) 
            ?? throw new UnauthorizedAccessException("Invalid access token");

        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _dbContext.Profiles.FindAsync(userId) 
            ?? throw new UnauthorizedAccessException("User not found");

        if (profile.RefreshTokenExpiryTime <= DateTime.UtcNow || 
            !BCrypt.Net.BCrypt.Verify(refreshToken, profile.RefreshTokenHash))
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var tokens = await UpdateTokensAsync(profile);
        await _dbContext.SaveChangesAsync();

        return tokens;
    }

    public async Task LogoutAsync(Guid userId)
    {
        var profile = await _dbContext.Profiles.FindAsync(userId);
        if (profile != null)
        {
            profile.RefreshTokenHash = null;
            profile.RefreshTokenExpiryTime = null;
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task<AuthResponse> UpdateTokensAsync(Profile profile)
    {
        var accessToken = _tokenService.GenerateAccessToken(profile.Id, profile.Email, profile.Role);
        var refreshToken = _tokenService.GenerateRefreshToken();

        profile.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
        profile.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days

        return new AuthResponse(accessToken, refreshToken);
    }
}

```

---

### Step 5: Configure `appsettings.json` and `Program.cs`

**1. `appsettings.json**`

```json
{
  "JwtSettings": {
    "Secret": "ReplaceThisWithAVeryLongSecretKeyForHMAC256!!",
    "Issuer": "TaskedApi",
    "Audience": "TaskedApp",
    "AccessTokenExpiryMinutes": "15"
  }
}

```

**2. `src/Host/WebApi/Program.cs**`

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Infra.Auth;
using Users.Services;

var builder = WebApplication.CreateBuilder(args);

// Dependency Injection
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AuthService>();

// JWT Authentication Setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ClockSkew = TimeSpan.Zero // Tokens expire exactly on time
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication(); // MUST be before Authorization
app.UseAuthorization();

app.MapControllers();
app.Run();

```

---

### Step 6: Auth Controller & Protected Endpoints

Finally, expose the endpoints in your Web API.

**`src/Host/WebApi/Controllers/AuthController.cs`**

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Users.Services;

namespace WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpDto req)
    {
        var result = await _authService.SignUpAsync(req.Email, req.Password, req.Role, req.FirstName, req.LastName);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto req)
    {
        var result = await _authService.LoginAsync(req.Email, req.Password);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshDto req)
    {
        var result = await _authService.RefreshTokenAsync(req.AccessToken, req.RefreshToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize] // Requires access token to logout
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _authService.LogoutAsync(userId);
        return NoContent();
    }
}

// DTOs
public record SignUpDto(string Email, string Password, string Role, string FirstName, string LastName);
public record LoginDto(string Email, string Password);
public record RefreshDto(string AccessToken, string RefreshToken);

```

### RBAC Usage Example:

To protect your `Tasks` module endpoints, just use the standard `[Authorize]` attribute:

```csharp
[ApiController]
[Route("api/taskgroups")]
public class TaskGroupController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "User,Admin")] // Both can read
    public IActionResult Get() 
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok("Your tasks..."); 
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")] // ONLY Admins can delete
    public IActionResult Delete(Guid id) 
    {
        return Ok("Deleted."); 
    }
}

```