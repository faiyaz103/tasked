using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Users.Entities;

[Index(nameof(Email), IsUnique = true)] // Enforces unique index in PostgreSQL
[Index(nameof(Name))]                   // Standard index for faster name lookups
public class User
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}