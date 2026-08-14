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
            TimeSpan.FromDays(double.Parse(_configuration["JwtSettings:RefreshExpiresInDays"]!)));
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
}