using Microsoft.AspNetCore.Authentication.JwtBearer;
using Aaron.Pina.Blog.Article._05.Shared;
using Aaron.Pina.Blog.Article._05.Server;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Claims;

using var rsa = RSA.Create(2048);
var rsaKey = new RsaSecurityKey(rsa);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(Configuration.JwtBearer.Options(rsa));
builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenRepository>();
builder.Services.AddDbContext<TokenDbContext>(Configuration.DbContext.Options);
builder.Services.Configure<TokenConfig>(builder.Configuration.GetSection(nameof(TokenConfig)));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<TokenDbContext>().Database.EnsureCreated();

app.MapGet("/register", () => Results.Ok(Guid.NewGuid()))
   .AllowAnonymous();

app.MapGet("/token", (IOptionsSnapshot<TokenConfig> config, TokenRepository repository, Guid userId) =>
    {
        var existing = repository.TryGetTokenByUserId(userId);
        if (existing is not null)
        {
            return Results.BadRequest(new
            {
                Error = "User already has an active token",
                Message = "Use the /refresh endpoint with your refresh token to get a new token"
            });
        }
        var now = DateTime.UtcNow;
        var exp = now.AddMinutes(config.Value.TokenLifetime);
        var entity = new TokenEntity
        {
            UserId = userId,
            ExpiresAt = exp,
            CreatedAt = now,
            RefreshToken = TokenGenerator.GenerateRefreshToken(),
            Token = TokenGenerator.GenerateToken(rsaKey, userId, now, config.Value.TokenLifetime)
        };
        repository.SaveToken(entity);
        return Results.Ok(entity.ToResponse());
    })
   .AllowAnonymous();

app.MapPost("/refresh", (IOptionsSnapshot<TokenConfig> config, HttpContext context, TokenRepository repository) =>
    {
        var refreshToken = context.Request.Form["refresh_token"].FirstOrDefault();
        if (string.IsNullOrEmpty(refreshToken)) return Results.BadRequest();
        var existing = repository.TryGetTokenByRefreshToken(refreshToken);
        if (existing is null) return Results.Unauthorized();
        var now = DateTime.UtcNow;
        var exp = now.AddMinutes(config.Value.TokenLifetime);
        existing.ExpiresAt = exp;
        existing.RefreshToken = TokenGenerator.GenerateRefreshToken();
        existing.Token = TokenGenerator.GenerateToken(rsaKey, existing.UserId, now, config.Value.TokenLifetime);
        repository.UpdateToken(existing);
        return Results.Ok(existing.ToResponse());
    })
   .AllowAnonymous();

app.MapGet("/user", (HttpContext context) =>
    {
        if (!long.TryParse(context.User.FindFirstValue("exp"), out var exp)) return Results.Unauthorized();
        if (!Guid.TryParse(context.User.FindFirstValue("sub"), out var userId)) return Results.Unauthorized();
        var response = new UserResponse(userId, DateTime.UtcNow, DateTimeOffset.FromUnixTimeSeconds(exp).DateTime);
        return Results.Ok(response);
    })
   .RequireAuthorization();

app.Run();
