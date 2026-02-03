using Aaron.Pina.Blog.Article._05.Client;
using Aaron.Pina.Blog.Article._05.Shared;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("server-api", Configuration.TokenServer.HttpClientSettings)
                .ConfigurePrimaryHttpMessageHandler(Configuration.TokenServer.HttpMessageHandlerSettings)
                .AddHttpMessageHandler<TokenRefreshHandler>();
builder.Services.AddHostedService<TokenRefresherService>();
builder.Services.AddScoped<TokenRefreshHandler>();
builder.Services.AddSingleton<TokenStore>();

var app = builder.Build();

app.MapGet("/login", async (IHttpClientFactory factory, TokenStore tokenStore) =>
{
    var client = factory.CreateClient("server-api");
    using var registerResponse = await client.GetAsync("/register");
    if (!registerResponse.IsSuccessStatusCode) return Results.BadRequest("Unable to register");
    var userId = await registerResponse.Content.ReadFromJsonAsync<Guid>();
    if (userId == Guid.Empty) return Results.BadRequest("Unable to parse user id");
    using var tokenResponse = await client.GetAsync($"/token?userId={userId}");
    if (!tokenResponse.IsSuccessStatusCode) return Results.BadRequest("Unable to get token");
    var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
    if (token is null) return Results.BadRequest("Unable to parse token");
    tokenStore.AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(token.AccessTokenExpiresIn);
    tokenStore.RefreshToken = token.RefreshToken;
    tokenStore.AccessToken = token.AccessToken;
    return Results.Ok("Logged in");
});

app.MapGet("/info", async (IHttpClientFactory factory, TokenStore store) =>
{
    var client = factory.CreateClient("server-api");
    if (client.BaseAddress is null) return Results.BadRequest("Unable to get base address");
    var uriBuilder = new UriBuilder(client.BaseAddress) { Path = "user" };
    using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", store.AccessToken);
    using var response = await client.SendAsync(request);
    if (!response.IsSuccessStatusCode) return Results.BadRequest("Unable to get user info");
    var user = await response.Content.ReadFromJsonAsync<UserResponse>();
    if (user is null) return Results.BadRequest("Unable to parse user info");
    return Results.Ok($"User Id: {user.UserId}");
});

app.Run();
