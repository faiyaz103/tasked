using Shared.Infra.Enums;

namespace Users.Entities;

public class UserEntity
{
    public Guid Id {get; set;}
    public string Email {get; set;} = string.Empty;
    public Roles Role {get; set;} = Roles.User;

    public Profile? Profile {get; set;}

    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;
}