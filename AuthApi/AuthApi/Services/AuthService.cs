using System.Text.Json;
using AuthApi.API.Dtos;
using AuthApi.API.Entities;
using AuthApi.API.Interfaces;
using AuthApi.API.Options;
using Microsoft.Extensions.Options;

namespace AuthApi.API.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;
    
    public AuthService(IOptions<KeycloakOptions> keycloakOptions, IUserRepository userRepository, IHttpClientFactory httpClientFactory)
    {
        _userRepository = userRepository;
        _httpClientFactory = httpClientFactory;
        _keycloakOptions = keycloakOptions.Value;
    }

    public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("KeycloakClient");
        
        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "client_id", _keycloakOptions.ClientId },
            { "client_secret", _keycloakOptions.ClientSecret },
            { "code", code },
            { "redirect_uri", _keycloakOptions.RedirectUri }
        };

        var response = await httpClient.PostAsync(
            $"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(data),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to exchange the code for tokens. Status: {response.StatusCode}");
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

        return tokens ?? throw new InvalidOperationException("Keycloak returned empty response.");
    }

    public async Task<bool> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var adminToken = await GetAdministratorToken(cancellationToken);
    
        using var httpClient = _httpClientFactory.CreateClient("KeycloakClient");
        
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        
        var userPayload = new
        {
            username = request.Username,
            email = request.Email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new 
                { 
                    type = "password", 
                    value = request.Password, 
                    temporary = false 
                }
            }
        };
        
        var response = await httpClient.PostAsJsonAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/users", 
            userPayload, 
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to create user in Keycloak: {response.StatusCode}");
        }
        
        var locationHeader = response.Headers.Location;
        if (locationHeader == null)
        {
            throw new InvalidOperationException("Keycloak did not return the Location header with the user ID.");
        }

        var keycloakUserId = locationHeader.Segments.Last().TrimEnd('/');

        var roleName = request.Role.ToString();
        var roleResponse = await httpClient.GetAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/roles/{roleName}", 
            cancellationToken);

        if (!roleResponse.IsSuccessStatusCode)
        {
            throw new KeyNotFoundException($"Role '{roleName}' not found in Keycloak.");
        }

        var roleJson = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
        using var roleDoc = JsonDocument.Parse(roleJson);

        var roleToAssign = new[]
        {
            new
            {
                id = roleDoc.RootElement.GetProperty("id").GetString(),
                name = roleDoc.RootElement.GetProperty("name").GetString()
            }
        };

        var assignRoleResponse = await httpClient.PostAsJsonAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/users/{keycloakUserId}/role-mappings/realm", 
            roleToAssign, 
            cancellationToken);

        if (!assignRoleResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to assign the role '{roleName}' to the user in Keycloak.");
        }

        var entity = new User
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakUserId,
            Username = request.Username,
            Email = request.Email,
            Role = request.Role,
        };

        await _userRepository.AddAsync(entity, cancellationToken);

        return true;
    }

    public async Task SignOutUserAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("KeycloakClient");

        var data = new Dictionary<string, string>
        {
            { "client_id", _keycloakOptions.ClientId },
            { "client_secret", _keycloakOptions.ClientSecret },
            { "refresh_token", refreshToken }
        };

        var response = await httpClient.PostAsync(
            $"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/logout",
            new FormUrlEncodedContent(data),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Error during signing out. Status: {response.StatusCode}");
        }
    }

    public string GetAuthorizationRequestUrl()
    {
        var baseUrl = _keycloakOptions.BaseUrl;
        var redirectUri = Uri.EscapeDataString(_keycloakOptions.RedirectUri);

        return $"{baseUrl}/realms/{_keycloakOptions.Realm}/protocol/openid-connect/auth" +
               $"?client_id={_keycloakOptions.ClientId}" +
               $"&response_type=code" +
               $"&scope=openid" +
               $"&redirect_uri={redirectUri}";
    }

    private async Task<string> GetAdministratorToken(CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient("KeycloakClient");
    
        var data = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _keycloakOptions.ClientId },
            { "client_secret", _keycloakOptions.ClientSecret }
        };

        var response = await httpClient.PostAsync(
            $"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/token", 
            new FormUrlEncodedContent(data), 
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Cannot get Admin Token Keycloak. Status: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }
}