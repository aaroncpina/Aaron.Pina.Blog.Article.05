using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Buffers.Text;

namespace Aaron.Pina.Blog.Article._05.Server;

public static class TokenGenerator
{
    public static string GenerateToken(RsaSecurityKey rsaKey, Guid userId, DateTime now, double expiresIn)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            IssuedAt = now,
            Issuer = "https://localhost",
            Audience = "https://localhost",
            Expires = now.AddMinutes(expiresIn),
            Subject = new ClaimsIdentity([new Claim("sub", userId.ToString())]),
            SigningCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public static string GenerateRefreshToken(int length = 32) =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(length));
}
