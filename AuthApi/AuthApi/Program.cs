using AuthApi.API.Data;
using AuthApi.API.Dtos;
using AuthApi.API.Interfaces;
using AuthApi.API.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AuthApiDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<AdminTokenResponse>();

builder.Services.AddHttpClient("KeycloakClient", client =>
{
    var keycloakBaseUrl = builder.Configuration["Keycloak:BaseUrl"];
    client.BaseAddress = new Uri(keycloakBaseUrl!);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();