using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Infra.Auth;

public class TokenService: ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string email, string role)
    {
        return GenerateJwt(userId, email, role, 
            _configuration["JwtSettings:AccessSecret"]!, 
            TimeSpan.FromMinutes(double.Parse(_configuration["JwtSettings:AccessExpiresInMinutes"]!)));
    }

    public string GenerateRefreshToken(Guid userId, string email, string role)
    {
        return GenerateJwt(userId, email, role, 
            _configuration["JwtSettings:RefreshSecret"]!, 
            TimeSpan.FromMinutes(double.Parse(_configuration["JwtSettings:RefreshExpiresInMinutes"]!)));
    }

    private string GenerateJwt(Guid userId, string email, string role, string secret, TimeSpan expiresIn)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Standard claims + JTI for uniqueness
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal ValidateRefreshToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:RefreshSecret"]!);

        // Validates signature and lifetime strictly (Throws SecurityTokenException if expired or invalid)
        return tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Strict expiration check
        }, out _);
    }

    // Safe helper to extract UserId even if the token's lifetime is expired
    public Guid? ExtractUserIdFromUnvalidatedToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userIdStr = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdStr, out var userId) ? userId : null;
        }
        catch
        {
            return null; // Return null if the string isn't even a valid JWT structure
        }
    }
}