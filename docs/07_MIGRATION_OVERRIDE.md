To implement the **Migration Override** approach in a Modular Monolith, we first need to completely decouple your C# classes so the compiler doesn't throw circular dependency errors. Then, we will tell PostgreSQL to enforce the relationship at the database level.

Here is the complete step-by-step guide with the corrected code.

### Step 1: Clean Up the `Users` Module

Remove any reference to the `Tasks` module. The `Users` module should not know what a task is.

**1. Updated `Profile.cs**`

```csharp
using System.ComponentModel.DataAnnotations;
using Shared.Infra.Enums;

namespace Users.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Other;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // REMOVED: TaskGroups navigation property and using Tasks.Entities;
}

```

**2. Updated `ProfileEntityConfig.cs**`
*(This remains mostly the same, just confirming no task configurations are present).*

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Users.Entities.Configuration;

public class ProfileEntityConfig : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Gender).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.FirstName);
        builder.HasIndex(p => p.LastName);
        builder.HasIndex(p => p.Phone).IsUnique();
    }
}

```

---

### Step 2: Clean Up the `Tasks` Module

Remove the `Profile` navigation property. We will only keep the primitive `ProfileId` to act as our logical foreign key.

**3. Updated `TaskGroup.cs**`

```csharp
namespace Tasks.Entities;

public class TaskGroup
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    
    // This primitive Guid is all we need to link back to the Profile
    public Guid ProfileId { get; set; } 
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // REMOVED: Profile navigation property and using Users.Entities;
}

```

**4. Updated `TaskGroupEntityConfig.cs**`
Remove the `.HasOne().WithMany()` block entirely. Instead, make sure `ProfileId` is required and indexed for fast lookups.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tasks.Entities.Configurations;

public class TaskGroupEntityConfig : IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.ToTable("taskgroups");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(100);
        
        // Explicitly require the primitive Foreign Key
        builder.Property(t => t.ProfileId).IsRequired();
        
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => t.Title);
        
        // Add an index so querying Tasks by a User is lightning fast
        builder.HasIndex(t => t.ProfileId);
        
        // REMOVED: builder.HasOne(...) relationship mapping
    }
}

```

---

### Step 3: Generate the Migrations

1. First, make sure your `Users` module migrations are fully generated and applied so the `profiles` table exists in PostgreSQL.
2. Next, generate a new migration for the `Tasks` module.

Open your terminal and run:

```bash
dotnet ef migrations add AddTaskGroupWithProfileFk --project src/Modules/Tasks --startup-project src/WebApi

```

*(Adjust the folder paths if your solution structure is slightly different).*

---

### Step 4: Override the Migration with Raw SQL

Open the migration file that EF Core just generated in your `Tasks` project (it will be named something like `2024XXXX_AddTaskGroupWithProfileFk.cs`).

Because we removed the C# relationships, EF Core doesn't know these tables are linked. We must add the SQL manually to the `Up` and `Down` methods to tell PostgreSQL to enforce the constraint.

Modify the file to look like this:

```csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tasks.Migrations
{
    public partial class AddTaskGroupWithProfileFk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF Core's auto-generated code for creating the taskgroups table will be here...
            // ... (keep whatever code EF Core generated for creating the table) ...

            // ADD THIS AT THE VERY END OF THE Up() METHOD:
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                ADD CONSTRAINT ""FK_taskgroups_profiles_ProfileId"" 
                FOREIGN KEY (""ProfileId"") 
                REFERENCES profiles(""Id"") 
                ON DELETE CASCADE;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ADD THIS AT THE VERY BEGINNING OF THE Down() METHOD:
            migrationBuilder.Sql(@"
                ALTER TABLE taskgroups 
                DROP CONSTRAINT ""FK_taskgroups_profiles_ProfileId"";
            ");

            // EF Core's auto-generated code for dropping the table will be here...
            // ... (keep whatever code EF Core generated) ...
        }
    }
}

```

*Note on PostgreSQL syntax: We wrap the column names in double quotes (`"ProfileId"`, `"Id"`) because PostgreSQL automatically lowercases everything unless you explicitly wrap them in quotes, which matches how EF Core usually maps standard PascalCase properties.*

---

### Step 5: Update the Database

Finally, apply this modified migration to your PostgreSQL database:

```bash
dotnet ef database update --project src/Modules/Tasks --startup-project src/WebApi

```

Your architecture is now perfectly balanced. Your C# code is fully modular and decoupled, but your database is relational and strictly protects against inserting orphaned `TaskGroup` records.

---

Here is the exact SQL query you can run in pgAdmin4. It queries PostgreSQL's internal system catalogs to give you a clean, readable list of every constraint (Foreign Keys, Primary Keys, Unique constraints) in your database.

Open the **Query Tool** in pgAdmin4, paste this query, and execute it:

```sql
SELECT
    conrelid::regclass AS table_name,
    conname AS constraint_name,
    CASE contype
        WHEN 'c' THEN 'CHECK'
        WHEN 'f' THEN 'FOREIGN KEY'
        WHEN 'p' THEN 'PRIMARY KEY'
        WHEN 'u' THEN 'UNIQUE'
        WHEN 'x' THEN 'EXCLUDE'
    END AS constraint_type,
    pg_get_constraintdef(c.oid) AS constraint_definition
FROM
    pg_constraint c
JOIN
    pg_namespace n ON n.oid = c.connamespace
WHERE
    n.nspname = 'public' 
    AND conrelid::regclass::text NOT LIKE 'pg_%' 
ORDER BY
    conrelid::regclass::text, 
    contype DESC;

```

### How to read the results:

* **`table_name`**: The table the constraint belongs to (e.g., `taskgroups` or `profiles`).
* **`constraint_name`**: The name EF Core or you gave the constraint (e.g., `fk_taskgroups_profiles_profile_id`).
* **`constraint_type`**: Tells you immediately if it is a Primary Key, Foreign Key, etc.
* **`constraint_definition`**: This is the most useful column. It shows you the exact rule PostgreSQL is enforcing (e.g., `FOREIGN KEY (profile_id) REFERENCES profiles(id) ON DELETE CASCADE`).

If your migration was successful, you will clearly see `fk_taskgroups_profiles_profile_id` listed under the `taskgroups` table.