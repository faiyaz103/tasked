using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // 1. Added namespace
using Microsoft.Extensions.DependencyInjection;
using Users.Dtos;
using Users.Entities;
using Users.Services;

namespace Users;

public static class UserModule
{
    // 2. Added 'IConfiguration configuration' parameter
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {   
        // Register PostgreSQL DbContext
        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")).UseSnakeCaseNamingConvention());

        // Register Services
        services.AddScoped<IUserService, UserService>();

        // Register Validators
        // Registers all FluentValidation validators inside the Users assembly
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();
        return services;
    }
}