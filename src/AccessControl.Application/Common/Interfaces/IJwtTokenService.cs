namespace AccessControl.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(
        Guid userId, string email, string fullName, IList<string> roles, Guid? tenantId);

    (string RawToken, string TokenHash) GenerateRefreshToken();

    string HashRefreshToken(string rawToken);
}
