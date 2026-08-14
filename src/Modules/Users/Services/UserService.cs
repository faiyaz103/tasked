using Microsoft.EntityFrameworkCore;
using Shared.Infra.Auth;
using Shared.Infra.Enums;
using Users.Dtos;
using Users.Entities;

namespace Users.Services;

public interface IUserService
{
    string GetHelloMessage();

    Task<(Guid, ProfileRespone)> CreateProfileAsync(CreateProfileRequest request);
    Task<string> CreateUserAsync(CreateUserRequest request);
    Task<TokenResponse> CreateUserSignInAsync(SignInUserRequest request);
    Task<ProfileRespone?> GetProfileAsync(Guid id);
}

public class UserService: IUserService
{
    private readonly UsersDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public UserService(UsersDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }
    public string GetHelloMessage()
    {
        return "Hello from .NET !";
    }

    // -----------------User--------------------
    // create
    public async Task<string> CreateUserAsync(CreateUserRequest request)
    {
        // validate email
        var emailExists = await _dbContext.Users.AnyAsync(u=> u.Email == request.Email);
        if (emailExists)
        {
            throw new InvalidOperationException("This email already exists");
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        Roles userRole = string.IsNullOrWhiteSpace(request.Role) ? Roles.User : Enum.Parse<Roles>(request.Role, ignoreCase: true);

        var newUser = new UserEntity
        {
            Email = request.Email,
            Password = hashedPassword,
            Role = userRole
        };

        _dbContext.Users.Add(newUser);
        await _dbContext.SaveChangesAsync();

        return $"Registration Successful for {request.Email}";
    }

    // login
    public async Task<TokenResponse> CreateUserSignInAsync(SignInUserRequest request)
    {
        // validate email
        var user = await _dbContext.Users.FirstOrDefaultAsync(u=> u.Email == request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        bool isPassValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!isPassValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // geenrate tokens concurrently
        var accessTokenTask = Task.Run(()=>
            _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString())
        );
        var refreshTokenTask = Task.Run(()=>
            _tokenService.GenerateRefreshToken(user.Id, user.Email, user.Role.ToString())
        );

        await Task.WhenAll(accessTokenTask, refreshTokenTask);

        string accessToken = accessTokenTask.Result;
        string refreshToken = refreshTokenTask.Result;

        // Double hash the refresh token (CPU bound)
        string hashedRefreshToken = await Task.Run(()=>
            TokenSecurityHelper.DoubleHashToken(refreshToken)
        );

        // save refresh token in DB
        user.RefreshToken = hashedRefreshToken;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        return new TokenResponse(accessToken, refreshToken);
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