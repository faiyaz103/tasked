using Microsoft.Extensions.DependencyInjection;
using Users.Services;

namespace Users;

public static class UserModule
{
    public static IServiceCollection AddUserModule(this IServiceCollection services)
    {
        // reg services
        services.AddScoped<IUserService, UserService>();

        // reg validators
        return services;
    }
}