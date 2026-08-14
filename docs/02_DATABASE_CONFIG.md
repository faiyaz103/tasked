### Step 1: Install Required NuGet Packages
```
# Install EF Core global CLI tool (if not already installed)
dotnet tool install --global dotnet-ef

# Install PostgreSQL provider in the Users module
dotnet add src/Modules/Users/Users.csproj package Npgsql.EntityFrameworkCore.PostgreSQL

# Install EF Core Design in the WebApi host (required for generating migrations)
dotnet add src/Host/WebApi/WebApi.csproj package Microsoft.EntityFrameworkCore.Design
```
---
### Step 2: Add Database Connection String
Update `src/Host/WebApi/appsettings.json` with your PostgreSQL database credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=modular_db;Username=postgres;Password=your_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
---
### Step 3: Create the DbContext
Create `src/Modules/Users/Entities/UsersDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;

namespace Users.Entities;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Auto-applies constraints defined in User class/configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
```
---
### Step 4: Register PostgreSQL in Module & Host
Update s`rc/Modules/Users/UsersModule.cs` to accept `IConfiguration` and configure `UsersDbContext`:
```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Dtos;
using Users.Entities;
using Users.Services;

namespace Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register PostgreSQL DbContext
        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // 2. Register Module Services
        services.AddScoped<IUserService, UserService>();
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        return services;
    }
}
```
Then update `src/Host/WebApi/Program.cs` to pass `builder.Configuration`:
```csharp
using Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register Module with Configuration passed in
builder.Services.AddUsersModule(builder.Configuration);

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

app.UseCors("AllowFrontend");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();
```
---
### Step 5: Apply Migrations & Run
```
dotnet ef migrations add AddUsersTable --project src/Modules/Users/Users.csproj --startup-project src/Host/WebApi/WebApi.csproj
dotnet ef database update --project src/Modules/Users/Users.csproj --startup-project src/Host/WebApi/WebApi.csproj
```
---
### Step 6: Revert the Database Schema
- To roll back the database completely (remove all applied migrations):
```
dotnet ef database update 0 --project src/Modules/Users/Users.csproj --startup-project src/Host/WebApi/WebApi.csproj
```
- To roll back to a specific previous migration:
```
dotnet ef database update <PreviousMigrationName> --project src/Modules/Users/Users.csproj --startup-project src/Host/WebApi/WebApi.csproj
```
---
### Step 7: Delete the Migration Files from Code
```
dotnet ef migrations remove --project src/Modules/Users/Users.csproj --startup-project src/Host/WebApi/WebApi.csproj
```
### Make EF Core use standard PostgreSQL snake_case (Recommended)
- Install the Naming Conventions package:
```
dotnet add src/Modules/Users/Users.csproj package EFCore.NamingConventions
```
- Enable it in UserModule.cs:
```csharp
services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention()); // <-- Add this
```