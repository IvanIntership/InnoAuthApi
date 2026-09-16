using AuthApi.API.Dtos;

namespace AuthApi.API.Interfaces;

public interface IAuthService
{
    Task<bool> RegisterUserAsync(RegisterUserRequest request, Guid? customAccountId = null, CancellationToken cancellationToken = default);
    Task<TokenResponse> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken);
    Task SignOutUserAsync(string refreshToken, CancellationToken cancellationToken);
    string GetAuthorizationRequestUrl();
}