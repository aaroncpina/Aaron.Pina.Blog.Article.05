namespace Aaron.Pina.Blog.Article._05.Shared;

public record TokenResponse(string AccessToken, string RefreshToken, double AccessTokenExpiresIn);
