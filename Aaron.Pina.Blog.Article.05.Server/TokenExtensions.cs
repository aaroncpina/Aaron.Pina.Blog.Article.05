using Aaron.Pina.Blog.Article._05.Shared;

namespace Aaron.Pina.Blog.Article._05.Server;

public static class TokenExtensions
{
    extension(TokenEntity token)
    {
        public TokenResponse ToResponse() => 
            new(token.Token, token.RefreshToken, token.ExpiresAt.Subtract(DateTime.UtcNow).TotalSeconds);
    }
}
