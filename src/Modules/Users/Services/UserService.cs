using Microsoft.EntityFrameworkCore;
using Users.Dtos;
using Users.Entities;

namespace Users.Services;

public interface IUserService
{
    string GetHelloMessage();
    Task<UserResponse> CreateUserAsync(CreateUserRequest request);
    Task<UserResponse?> GetByIdAsync(Guid id);
}

public class UserService: IUserService
{
    private readonly UsersDbContext _dbContext;

    public UserService(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public string GetHelloMessage()
    {
        return "Hello from .NET !";
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        // validate email
        var emailExists = await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
        {
            throw new InvalidOperationException("Email already exists");
        }

        // map dto to entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        // save to postgres
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return null;

        return new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
    }
}