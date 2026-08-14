using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;

namespace Tasks.Entities;

public class TasksDbContext: DbContext
{
    public TasksDbContext(DbContextOptions<TasksDbContext> options): base(options) {}
    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Auto-applies constraints defined in User class/configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
    }
}