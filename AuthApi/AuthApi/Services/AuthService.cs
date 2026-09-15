using System.Text.Json;
using AuthApi.API.Dtos;
using AuthApi.API.Entities;
using AuthApi.API.Interfaces;
using AuthApi.API.Options;
using InnoClinic.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Options;

namespace AuthApi.API.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly IPublishEndpoint _publishEndpoint;
    
    public AuthService(
        IOptions<KeycloakOptions> keycloakOptions, 
        IUserRepository userRepository, 
        IHttpClientFactory httpClientFactory, 
        IPublishEndpoint publishEndpoint)
    {
        _userRepository = userRepository;
        _httpClientFactory = httpClientFactory;
        _publishEndpoint = publishEndpoint;
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
            $"realms/{_keycloakOptions.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(data),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to exchange the code for tokens. Status: {response.StatusCode}");
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);

        return tokens ?? throw new InvalidOperationException("Keycloak returned empty response.");
    }

    public async Task<bool> RegisterUserAsync(RegisterUserRequest request, Guid? customAccountId = null, CancellationToken cancellationToken = default)
    {
        var adminToken = await GetAdministratorToken(cancellationToken);
        using var httpClient = _httpClientFactory.CreateClient("KeycloakClient");
        
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        var userPayload = new Dictionary<string, object>
        {
            { "username", request.Email },
            { "email", request.Email },
            { "firstName", request.Firstname },
            { "lastName", request.Lastname },
            { "enabled", true },
            { "emailVerified", true },
            { "credentials", new[] { new { type = "password", value = request.Password, temporary = false } } }
        };

        if (customAccountId.HasValue && customAccountId.Value != Guid.Empty)
        {
            userPayload["id"] = customAccountId.Value.ToString();
        }
        
        var response = await httpClient.PostAsJsonAsync(
            $"admin/realms/{_keycloakOptions.Realm}/users", 
            userPayload, 
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to create user in Keycloak: {response.StatusCode}, Details: {errorContent}");
        }
       
        var searchUserResponse = await httpClient.GetAsync(
            $"admin/realms/{_keycloakOptions.Realm}/users?email={Uri.EscapeDataString(request.Email)}&exact=true", 
            cancellationToken);

        if (!searchUserResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to find created user in Keycloak by email '{request.Email}'.");
        }

        var searchUsersJson = await searchUserResponse.Content.ReadAsStringAsync(cancellationToken);
        using var usersDoc = JsonDocument.Parse(searchUsersJson);
        var userArray = usersDoc.RootElement;

        if (userArray.GetArrayLength() == 0)
        {
            throw new KeyNotFoundException($"User with email '{request.Email}' was not found in Keycloak after creation.");
        }

        var keycloakInternalId = userArray[0].GetProperty("id").GetString();

        Guid finalUserId = customAccountId.HasValue && customAccountId.Value != Guid.Empty
            ? customAccountId.Value
            : (Guid.TryParse(keycloakInternalId, out var parsedGuid) 
                ? parsedGuid 
                : throw new InvalidOperationException("Keycloak did not return a valid Guid for user."));

        var roleName = request.Role.ToString();
        var roleResponse = await httpClient.GetAsync(
            $"admin/realms/{_keycloakOptions.Realm}/roles/{roleName}", 
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
            $"admin/realms/{_keycloakOptions.Realm}/users/{keycloakInternalId}/role-mappings/realm", 
            roleToAssign, 
            cancellationToken);

        if (!assignRoleResponse.IsSuccessStatusCode)
        {
            var assignError = await assignRoleResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to assign the role '{roleName}' to user in Keycloak. Status: {assignRoleResponse.StatusCode}, Details: {assignError}");
        }
        
        var entity = new User
        {
            Id = finalUserId,
            Username = request.Email,
            Email = request.Email,
            Role = request.Role,
        };
        
        await _userRepository.AddAsync(entity, cancellationToken);

        if (request.Role == Roles.Patient && !customAccountId.HasValue)
        {
            await _publishEndpoint.Publish<IPatientRegisteredEvent>(new
            {
                AccountId = finalUserId,
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Password = request.Password,
                Birthday = request.Birthday
            }, cancellationToken);
        }
        
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
            $"realms/{_keycloakOptions.Realm}/protocol/openid-connect/logout",
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
            $"realms/{_keycloakOptions.Realm}/protocol/openid-connect/token", 
            new FormUrlEncodedContent(data), 
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Keycloak error ({response.StatusCode}): {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }
}