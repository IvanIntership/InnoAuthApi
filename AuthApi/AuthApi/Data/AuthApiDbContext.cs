using AuthApi.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.API.Data;

public sealed class AuthApiDbContext : DbContext
{
    public AuthApiDbContext(DbContextOptions<AuthApiDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasKey(user => user.Id);
        
        modelBuilder.Entity<User>()
            .Property(user => user.Email)
            .HasMaxLength(256);
            
        modelBuilder.Entity<User>()   
            .HasIndex(user => user.Email)
            .IsUnique();
    }
}