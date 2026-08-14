using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Users.Entities.Configuration;

public class UserEntityConfig: IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u=>u.Id);

        builder.Property(u=>u.Email)
        .IsRequired()
        .HasMaxLength(100);

        builder.HasIndex(u=>u.Email)
        .IsUnique();

        builder.Property(u=>u.Password)
        .IsRequired()
        .HasMaxLength(256);

        builder.Property(u=>u.RefreshToken)
        .IsRequired(false)
        .HasColumnType("text");

        builder.Property(u=>u.Role)
        .IsRequired()
        .HasConversion<string>()
        .HasMaxLength(10);

        builder.Property(u=>u.CreatedAt)
        .IsRequired();

        builder.Property(u=>u.UpdatedAt)
        .IsRequired();
    }
}