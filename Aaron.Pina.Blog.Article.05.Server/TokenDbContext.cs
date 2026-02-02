using Microsoft.EntityFrameworkCore;

namespace Aaron.Pina.Blog.Article._05.Server;

public class TokenDbContext(DbContextOptions<TokenDbContext> options) : DbContext(options)
{
    public DbSet<TokenEntity> Tokens => Set<TokenEntity>();
}
