using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Shared.Infra.Enums;

namespace Users.Entities;

[Index(nameof(FirstName))]
[Index(nameof(LastName))]
[Index(nameof(Phone), IsUnique =true)]
public class Profile
{
    [Key]
    public Guid Id {get; set;}

    [MaxLength(100)]
    public string FirstName {get; set;} = string.Empty;

    [MaxLength(100)]
    public string LastName {get; set;} = string.Empty;

    [MaxLength(20)]
    public string Phone {get; set;} = string.Empty;

    public Gender Gender {get; set;} = Gender.Other;

    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
}