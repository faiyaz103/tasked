using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Infra.Auth;
using Shared.Infra.Enums;
using Tasks;
using Users;
using Users.Entities;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["AccessSecret"]!);

// 2. Configure Authentication
builder.Services.AddAuthentication(options =>
{
    // Sets JWT Bearer as the default authentication scheme
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        
        ValidateLifetime = true, // Ensures expired tokens are rejected
        
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        
        // Disables the default 5-minute clock skew so tokens expire EXACTLY when they are supposed to
        ClockSkew = TimeSpan.Zero 
    };
});

builder.Services.AddAuthorization(options =>
{
    // Policy 1: Only Admins
    options.AddPolicy("RequireAdminRole", policy => 
        policy.RequireRole(nameof(Roles.Admin)));

    options.AddPolicy("RequireUserRole", policy => 
        policy.RequireRole(nameof(Roles.User)));

    // Policy 2: Admins AND users (Example of multi-role policy)
    options.AddPolicy("RequireElevatedAccess", policy => 
        policy.RequireRole(nameof(Roles.Admin), nameof(Roles.User)));
});

// add controllers
builder.Services.AddControllers();

builder.Services.AddScoped<ITokenService, TokenService>();

// register modules
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddTasksModule(builder.Configuration);

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

// --- DATABASE CONNECTION CHECK ---
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

        // Attempts a ping connection to PostgreSQL
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("PostgreSQL Database connected successfully!");
        }
        else
        {
            logger.LogWarning("Failed to establish a connection to PostgreSQL Database.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while trying to connect to PostgreSQL Database.");
    }
}

// Configure middleware
app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// map controllers
app.MapControllers();

app.Run();
