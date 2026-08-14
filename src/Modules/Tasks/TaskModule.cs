using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tasks.Dtos;
using Tasks.Entities;
using Tasks.Services;

namespace Tasks;

public static class TaskModule
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services, IConfiguration configuration)
    {   
        // reg pg db context
        services.AddDbContext<TasksDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")).UseSnakeCaseNamingConvention());

        // reg services
        services.AddScoped<ITaskService, TaskService>();

        // reg validators
        services.AddValidatorsFromAssemblyContaining<CreateTaskValidator>();

        return services;

    }
}
