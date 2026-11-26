using System;
using System.Net.Http;

namespace DiscordMultiTool.Services;

public static class HttpClientProvider
{
    public static HttpClient Shared { get; } = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordMultiTool/3.5");
        return client;
    }
}
