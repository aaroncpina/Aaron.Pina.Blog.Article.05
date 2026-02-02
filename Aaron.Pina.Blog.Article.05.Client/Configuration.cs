namespace Aaron.Pina.Blog.Article._05.Client;

public static class Configuration
{
    public static class TokenServer
    {
        public static void HttpClientSettings(HttpClient client)
        {
            client.BaseAddress = new Uri("https://localhost:5001");
            client.Timeout = TimeSpan.FromSeconds(10);
        }

        public static HttpMessageHandler HttpMessageHandlerSettings() =>
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
    }
}
