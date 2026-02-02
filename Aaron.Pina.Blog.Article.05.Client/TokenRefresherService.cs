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
            try
            {
                using var scope = serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<TokenStore>();
                if (string.IsNullOrEmpty(store.Token)
                ||  string.IsNullOrEmpty(store.RefreshToken)
                ||  store.Expiry.Subtract(DateTime.UtcNow) > TimeSpan.FromMinutes(5))
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                var client = factory.CreateClient("server-api");
                var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5001/refresh");
                var content = new KeyValuePair<string, string>("refresh_token", store.RefreshToken);
                request.Content = new FormUrlEncodedContent([content]);
                using var response = await client.SendAsync(request, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Refresh failed with status {StatusCode}", response.StatusCode);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(stoppingToken);
                if (tokenResponse is null)
                {
                    logger.LogWarning("Refresh response was null or invalid");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                store.Expiry = DateTime.UtcNow.AddMinutes(tokenResponse.ExpiresIn);
                store.RefreshToken = tokenResponse.RefreshToken;
                store.Token = tokenResponse.Token;
                logger.LogInformation("Proactively refreshed token");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error in proactive refresh loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
