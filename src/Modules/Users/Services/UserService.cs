using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Shared.Infra.Auth;
using Shared.Infra.Enums;
using Users.Dtos;
using Users.Entities;

namespace Users.Services;

public interface IUserService
{
    Task<ProfileRespone> CreateProfileAsync(Guid id, CreateProfileRequest request);
    Task<string> CreateUserAsync(CreateUserRequest request);
    Task<TokenResponse> CreateUserSignInAsync(SignInUserRequest request);
    Task<TokenResponse> RotateTokensAsync(RotateTokenRequest request);
    Task<ProfileRespone> UpdateProfileAsync(Guid id, UpdateProfileRequest request);
    Task SignOutAsync(Guid userId);
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

    // logout
    public async Task SignOutAsync(Guid userId)
    {
        // 1. Find the user by ID
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null)
        {
            // 2. Revoke the session by removing the refresh token
            user.RefreshToken = null;
            
            // 3. Save changes
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }
    }

    // rotate tokens
    public async Task<TokenResponse> RotateTokensAsync(RotateTokenRequest request)
    {
        ClaimsPrincipal principal;

        // 1. Validate JWT Signature & Expiry
        try
        {
            principal = _tokenService.ValidateRefreshToken(request.Token);
        }
        catch
        {
            // IF EXPIRED OR INVALID: Invalidate DB session if user can be identified
            await InvalidateSessionIfPossibleAsync(request.Token);
            throw new UnauthorizedAccessException("Refresh token is expired or invalid. Session revoked.");
        }

        // 2. Extract UserId from claims
        // var userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
             ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            throw new UnauthorizedAccessException("Invalid token payload.");
        }

        // 3. Fetch User from Database
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || string.IsNullOrEmpty(user.RefreshToken))
        {
            throw new UnauthorizedAccessException("Access denied. Active session not found.");
        }

        // 4. Verify Double-Hash (SHA-256 + BCrypt)
        bool isTokenValid = TokenSecurityHelper.VerifyDoubleHashedToken(request.Token, user.RefreshToken);

        if (!isTokenValid)
        {
            // REUSE / THEFT DETECTED: Someone tried to use an old, rotated token!
            user.RefreshToken = null;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            throw new UnauthorizedAccessException("Security alert: Token reuse detected. Session revoked.");
        }

        // 5. Generate NEW Access & Refresh Tokens Concurrently
        var accessTokenTask = Task.Run(() => 
            _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString()));

        var refreshTokenTask = Task.Run(() => 
            _tokenService.GenerateRefreshToken(user.Id, user.Email, user.Role.ToString()));

        await Task.WhenAll(accessTokenTask, refreshTokenTask);

        string newAccessToken = accessTokenTask.Result;
        string newRefreshToken = refreshTokenTask.Result;

        // 6. Double-Hash NEW Refresh Token & Update Database
        user.RefreshToken = await Task.Run(() => TokenSecurityHelper.DoubleHashToken(newRefreshToken));
        
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();

        // 7. Return new token pair to client
        return new TokenResponse(newAccessToken, newRefreshToken);
    }

    // Helper method to nuke session when invalid token is supplied
    private async Task InvalidateSessionIfPossibleAsync(string refreshToken)
    {
        Guid? userId = _tokenService.ExtractUserIdFromUnvalidatedToken(refreshToken);
        if (userId.HasValue)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (user != null && user.RefreshToken != null)
            {
                user.RefreshToken = null;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    // -------------------Profile----------------
    // create
    public async Task<ProfileRespone> CreateProfileAsync(Guid id, CreateProfileRequest request)
    {
        
        //  Check if user already has a profile (Enforces the 1-to-1 relationship)
        var profileExists = await _dbContext.Profiles.AnyAsync(p => p.UserId == id);
        if (profileExists)
        {
            throw new InvalidOperationException("User already has a profile.");
        }

        // validate phone
        var phoneExists = await _dbContext.Profiles.AnyAsync(
            u => u.Phone == request.phone
        );
        if (phoneExists)
        {
            throw new InvalidOperationException("Phone already exists");
        }

        Gender userGender = string.IsNullOrWhiteSpace(request.gender) ? Gender.Other : Enum.Parse<Gender>(request.gender, ignoreCase: true);

        // map dto to entity
        var profile = new Profile
        {
            FirstName = request.firstName,
            LastName = request.lastName,
            UserId = id,
            Phone = request.phone,
            Gender = userGender
        };

        // create entity
        _dbContext.Profiles.Add(profile);

        // save to db
        await _dbContext.SaveChangesAsync();

        var profileRespone = new ProfileRespone(
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            userGender.ToString()
        );

        return profileRespone;
    }

    // get
    public async Task<ProfileRespone?> GetProfileAsync(Guid id)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p=>p.UserId == id);
        if(profile == null) return null;

        return new ProfileRespone(
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            profile.Gender.ToString()
        );
    }

    // update
    public async Task<ProfileRespone> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p=>p.UserId == id);
        if(profile == null)
        {
            throw new KeyNotFoundException("Profile does not exist.");
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            profile.FirstName = request.FirstName;
        }
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            profile.LastName = request.LastName;
        }
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            profile.Gender = Enum.Parse<Gender>(request.Gender, ignoreCase: true);
        }
        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone != profile.Phone)
        {
            var phoneExists = await _dbContext.Profiles.AnyAsync(p=>p.Phone == request.Phone);
            if (phoneExists)
            {
                throw new InvalidOperationException("phone number already exists");
            }
            profile.Phone = request.Phone;
        }

        profile.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return new ProfileRespone(
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            profile.Gender.ToString()
        );
    }
}