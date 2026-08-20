using AuthApi.API.Data;
using AuthApi.API.Entities;
using AuthApi.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.API.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AuthApiDbContext _dbContext;

    public UserRepository(AuthApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}