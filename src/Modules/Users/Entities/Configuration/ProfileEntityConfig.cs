using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Users.Entities.Configuration;

public class ProfileEntityConfig: IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p=>p.Id);

        builder.Property(p=>p.FirstName)
        .IsRequired()
        .HasMaxLength(100);

        builder.Property(p=>p.LastName)
        .IsRequired()
        .HasMaxLength(100);

        builder.Property(p=>p.Gender)
        .IsRequired()
        .HasConversion<string>()
        .HasMaxLength(10);

        builder.Property(p=>p.CreatedAt)
        .IsRequired();

        builder.Property(p=>p.UpdatedAt)
        .IsRequired();

        builder.HasIndex(p=>p.FirstName);
        builder.HasIndex(p=>p.LastName);
        builder.HasIndex(p=>p.Phone).IsUnique();

        builder.HasOne(p=>p.User)
        .WithOne(u=>u.Profile)
        .HasForeignKey<Profile>(p=>p.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    }
}