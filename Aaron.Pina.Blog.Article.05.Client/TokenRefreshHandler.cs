using Aaron.Pina.Blog.Article._05.Shared;
using System.Net.Http.Headers;
using System.Net;

namespace Aaron.Pina.Blog.Article._05.Client;

public class TokenRefreshHandler(TokenStore store) : DelegatingHandler
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1); 
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (string.IsNullOrEmpty(store.Token) || string.IsNullOrEmpty(store.RefreshToken)) return response;
            if (store.Expiry < DateTime.UtcNow)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", store.Token);
                return await base.SendAsync(request, ct);
            }
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:5001/refresh");
            var content = new KeyValuePair<string, string>("refresh_token", store.RefreshToken);
            refreshRequest.Content = new FormUrlEncodedContent([content]);
            using var refreshResponse = await base.SendAsync(refreshRequest, ct);
            if (!refreshResponse.IsSuccessStatusCode) return response;
            var tokenResponse = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);
            if (tokenResponse is null) return response;
            store.Expiry = DateTime.UtcNow.AddMinutes(tokenResponse.ExpiresIn);
            store.RefreshToken = tokenResponse.RefreshToken;
            store.Token = tokenResponse.Token;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
            return await base.SendAsync(request, ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
