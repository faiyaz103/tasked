Entity classes stay as **pure, clean POCO (Plain Old C# Objects)**. They have zero knowledge of EF Core, zero attributes, zero database annotations. They are just simple C# classes with properties.

---

### What a Pure POCO Entity Looks Like

```csharp
// Entities/Profile.cs
namespace Tasks.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public ICollection<TaskGroup> TaskGroups { get; set; } = new List<TaskGroup>();
}
```

```csharp
// Entities/TaskGroup.cs
namespace Tasks.Entities;

public class TaskGroup
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
```

```csharp
// Entities/TaskItem.cs
namespace Tasks.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public Guid TaskGroupId { get; set; }
    public TaskGroup TaskGroup { get; set; } = null!;
}
```

No `[Table]`, no `[Required]`, no `[MaxLength]`, no `[ForeignKey]` attributes anywhere. **All of that lives in the Configuration classes.**

---

### Why This Matters: The Two Approaches Compared

There is an alternative approach many beginners use called **Data Annotations**, where you decorate entity properties with attributes directly. Here is why the Fluent API + Configuration approach wins:

```csharp
// ❌ Data Annotations approach (avoid this)
// Your entity becomes polluted with DB concerns
[Table("task_groups")]
public class TaskGroup
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [ForeignKey("Profile")]
    public Guid ProfileId { get; set; }
}
```

```csharp
// ✅ IEntityTypeConfiguration approach (industry standard)
// Entity is clean. Configuration class handles all DB concerns.
public class TaskGroup
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public Profile Profile { get; set; } = null!;
}
```

| | Data Annotations | `IEntityTypeConfiguration` |
|:---|:---|:---|
| Entity cleanliness | Polluted with DB attributes | Pure POCO, no DB knowledge |
| Capability | Limited, not everything is possible | Full power of Fluent API |
| Testability | Harder to unit test entities | Entities are easily testable |
| Separation of Concerns | Violated | Fully respected |
| Industry standard | Beginner-friendly shortcut | Enterprise standard |

---

### The Simple Rule to Remember

> **Entities describe your domain. Configuration classes describe how entities map to the database. These are two separate concerns and should live in two separate places.**

Your entities should be so clean that if you removed EF Core from the project entirely, they would still make perfect sense as plain C# classes.

Yes! The industry standard is to avoid putting all your Fluent API configuration directly inside `OnModelCreating` in the `DbContext`. As your project grows, `OnModelCreating` becomes a massive, unreadable blob of configuration code.

The better approach is **`IEntityTypeConfiguration<T>`** — a dedicated configuration class per entity.

---

### Why `IEntityTypeConfiguration<T>` is Better

| `OnModelCreating` (what we had) | `IEntityTypeConfiguration<T>` (better way) |
| :--- | :--- |
| All config crammed in one method | One file per entity |
| `DbContext` grows massive over time | `DbContext` stays small and clean |
| Hard to find specific entity config | Easy to navigate |
| Violates Single Responsibility Principle | Each class has one job |

---

### How to Do It

Create a dedicated `Configurations` folder inside your module and add one configuration class per entity.

#### 1. Profile Configuration
```csharp
// Tasks/Configurations/ProfileConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tasks.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Relationship is configured from the "child" side (TaskGroupConfiguration)
        // so nothing here about cascade
    }
}
```

#### 2. TaskGroup Configuration
```csharp
// Tasks/Configurations/TaskGroupConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tasks.Configurations;

public class TaskGroupConfiguration : IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.ToTable("task_groups");

        builder.HasKey(tg => tg.Id);

        builder.Property(tg => tg.Title)
            .IsRequired()
            .HasMaxLength(200);

        // CASCADE DELETE: Deleting a Profile deletes all its TaskGroups
        builder.HasOne(tg => tg.Profile)
            .WithMany(p => p.TaskGroups)
            .HasForeignKey(tg => tg.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 3. TaskItem Configuration
```csharp
// Tasks/Configurations/TaskItemConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tasks.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Content)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.IsCompleted)
            .HasDefaultValue(false);

        // CASCADE DELETE: Deleting a TaskGroup deletes all its Tasks
        builder.HasOne(t => t.TaskGroup)
            .WithMany(tg => tg.Tasks)
            .HasForeignKey(t => t.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

#### 4. The DbContext is Now Razor Thin and Clean
The magic is `ApplyConfigurationsFromAssembly`. It automatically discovers and applies **every** `IEntityTypeConfiguration<T>` class in the assembly, so you never have to manually register each one.

```csharp
// Tasks/TasksDbContext.cs
public class TasksDbContext : DbContext
{
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<TaskGroup> TaskGroups { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }

    public TasksDbContext(DbContextOptions<TasksDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This one line automatically picks up ALL IEntityTypeConfiguration classes
        // in this assembly. No need to register them manually one by one.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TasksDbContext).Assembly);
    }
}
```

---

### Final Folder Structure
```
Tasks/
├── Configurations/
│   ├── ProfileConfiguration.cs      ← config for Profile
│   ├── TaskGroupConfiguration.cs    ← config for TaskGroup + cascade rule
│   └── TaskItemConfiguration.cs     ← config for TaskItem + cascade rule
├── Entities/
│   ├── Profile.cs
│   ├── TaskGroup.cs
│   └── TaskItem.cs
├── TasksDbContext.cs                 ← clean, only 1 line in OnModelCreating
└── TaskModule.cs
```

This is the exact pattern used by large enterprise .NET projects. `ApplyConfigurationsFromAssembly` is the key — add a new entity configuration class, and it gets picked up **automatically** without touching `DbContext` again.