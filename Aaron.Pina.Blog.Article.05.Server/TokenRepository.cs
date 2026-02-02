namespace Aaron.Pina.Blog.Article._05.Server;

public class TokenRepository(TokenDbContext dbContext)
{
    public void SaveToken(TokenEntity token)
    {
        dbContext.Add(token);
        dbContext.SaveChanges();
    }

    public void UpdateToken(TokenEntity token)
    {
        dbContext.Update(token);
        dbContext.SaveChanges();
    }

    public TokenEntity? TryGetTokenByUserId(Guid userId) =>
        dbContext.Tokens.FirstOrDefault(t => t.UserId == userId);

    public TokenEntity? TryGetTokenByRefreshToken(string refreshToken) =>
        dbContext.Tokens.FirstOrDefault(t => t.RefreshToken == refreshToken);
}
