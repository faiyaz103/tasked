using Microsoft.Extensions.DependencyInjection;
using Tasks.Services;

namespace Tasks;

public static class TaskModule
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services)
    {   
        // reg services
        services.AddScoped<ITaskService, TaskService>();

        // reg dbcontext

        // reg validators

        return services;

    }
}
