using Microsoft.EntityFrameworkCore;
using Users.Dtos;
using Users.Entities;

namespace Users.Services;

public interface IUserService
{
    string GetHelloMessage();

    Task<(Guid, ProfileRespone)> CreateProfileAsync(CreateProfileRequest request);
    Task<ProfileRespone?> GetProfileAsync(Guid id);
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

    // -------------------Profile----------------
    // create
    public async Task<(Guid, ProfileRespone)> CreateProfileAsync(CreateProfileRequest request)
    {
        // validate phone
        var phoneExists = await _dbContext.Profiles.AnyAsync(
            u => u.Phone == request.phone
        );
        if (phoneExists)
        {
            throw new InvalidOperationException("Phone already exists");
        }

        // map dto to entity
        var profile = new Profile
        {
            FirstName = request.firstName,
            LastName = request.lastName,
            Phone = request.phone,
            Gender = request.gender
        };

        // create entity
        _dbContext.Profiles.Add(profile);

        // save to db
        await _dbContext.SaveChangesAsync();

        var profileRespone = new ProfileRespone(
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            profile.Gender
        );

        return (profile.Id, profileRespone);
    }

    // get
    public async Task<ProfileRespone?> GetProfileAsync(Guid id)
    {
        var profile = await _dbContext.Profiles.FindAsync(id);
        if(profile == null) return null;

        return new ProfileRespone(
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            profile.Gender
        );
    }
}