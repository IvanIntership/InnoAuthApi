using AuthApi.API.Data;
using AuthApi.API.Dtos;
using AuthApi.API.Interfaces;
using AuthApi.API.Options;
using AuthApi.API.Repositories;
using AuthApi.API.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AuthApiDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddValidatorsFromAssemblyContaining<AdminTokenResponse>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Auth API", 
        Version = "v1" 
    });
    
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",         
        BearerFormat = "JWT",
        Description = "Enter only JWT token"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    
    options.AddSecurityRequirement((doc) =>
    {
        var requirement = new OpenApiSecurityRequirement();
        var reference = new OpenApiSecuritySchemeReference("Bearer", doc);
        requirement[reference] = new List<string>(); 
        
        return requirement;
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var keycloakUrl = builder.Configuration["Keycloak:BaseUrl"]?.TrimEnd('/');
    var realm = builder.Configuration["Keycloak:Realm"];
    var authority = $"{keycloakUrl}/realms/{realm}";

    options.Authority = authority;
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authority,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

builder.Services.AddHttpClient("KeycloakClient", client =>
{
    var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
    
    if (string.IsNullOrWhiteSpace(keycloakBaseUrl))
    {
        throw new InvalidOperationException("Keycloak:BaseUrl configuration is missing.");
    }

    var normalizedUrl = keycloakBaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(normalizedUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();