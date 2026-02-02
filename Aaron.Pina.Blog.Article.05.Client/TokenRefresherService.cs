using Aaron.Pina.Blog.Article._05.Shared;

namespace Aaron.Pina.Blog.Article._05.Client;

public class TokenRefresherService(
    IHttpClientFactory factory,
    IServiceProvider serviceProvider,
    ILogger<TokenRefresherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            try
            {
                logger.LogInformation("Proactively checking expiry of tokens");
                using var scope = serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<TokenStore>();
                if (string.IsNullOrEmpty(store.RefreshToken)
                ||  string.IsNullOrEmpty(store.AccessToken)
                ||  store.ExpiresAt is null)
                {
                    logger.LogInformation("Tokens still uninitialised");
                    continue;
                }
                var expiresIn = store.ExpiresAt.Value.Subtract(DateTime.UtcNow);
                if (expiresIn > TimeSpan.FromMinutes(5))
                {
                    logger.LogInformation("Access token expires in {ExpiresIn} minutes", expiresIn.TotalMinutes);
                    continue;
                }
                var client = factory.CreateClient("server-api");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5001/refresh");
                var content = new KeyValuePair<string, string>("refresh_token", store.RefreshToken);
                request.Content = new FormUrlEncodedContent([content]);
                logger.LogInformation("Calling server to refresh tokens");
                using var response = await client.SendAsync(request, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Server refresh token response failed");
                    continue;
                }
                var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(stoppingToken);
                if (tokens is null)
                {
                    logger.LogWarning("Refresh token response content was invalid");
                    continue;
                }
                store.ExpiresAt = DateTime.UtcNow.AddMinutes(tokens.ExpiresIn);
                store.RefreshToken = tokens.RefreshToken;
                store.AccessToken = tokens.AccessToken;
                logger.LogInformation("Refreshed tokens");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error in proactive token refresh loop");
            }
        }
    }
}
