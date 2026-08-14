using Shared.Infra.Enums;

namespace Users.Entities;

public class Profile
{
    public Guid Id {get; set;}
    public string FirstName {get; set;} = string.Empty;
    public string LastName {get; set;} = string.Empty;
    public string Phone {get; set;} = string.Empty;
    public Gender Gender {get; set;} = Gender.Other;
    public Guid UserId {get; set;}
    public UserEntity User {get; set;} = null!;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
}