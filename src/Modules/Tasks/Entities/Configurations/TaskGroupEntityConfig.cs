using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tasks.Entities;

namespace Tasks.Entities.Configurations;

public class TaskGroupEntityConfig: IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.ToTable("taskgroups");

        builder.HasKey(t=>t.Id);

        builder.Property(t=>t.Title)
        .IsRequired()
        .HasMaxLength(100);

        builder.Property(t=>t.ProfileId)
        .IsRequired();

        builder.Property(t=>t.CreatedAt)
        .IsRequired();

        builder.Property(t=>t.UpdatedAt)
        .IsRequired();

        builder.HasIndex(t=>t.Title);
        builder.HasIndex(t=>t.ProfileId);

    }
}