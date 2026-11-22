using Microsoft.EntityFrameworkCore;
using PetID.Models;

namespace PetID.Data;

public class PetIdDbContext(DbContextOptions<PetIdDbContext> options) : DbContext(options)
{
    public DbSet<PetUser> Users => Set<PetUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PetUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });
    }
}
