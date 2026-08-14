namespace Shared.Infra.Auth;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken(Guid userId, string email, string role);
}