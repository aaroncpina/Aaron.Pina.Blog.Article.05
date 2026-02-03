using Microsoft.EntityFrameworkCore;

namespace Aaron.Pina.Blog.Article._05.Server;

public class TokenDbContext(DbContextOptions<TokenDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TokenEntity>()
                    .Property(t => t.RefreshToken)
                    .HasMaxLength(512);
    }
    
    public DbSet<TokenEntity> Tokens => Set<TokenEntity>();
}
