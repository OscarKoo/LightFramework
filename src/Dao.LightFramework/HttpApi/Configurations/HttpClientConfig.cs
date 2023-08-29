using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class HttpClientConfig
{
    public static IServiceCollection AddLightHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(Options.DefaultName, client => client.Timeout = Timeout.InfiniteTimeSpan).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            ServerCertificateCustomValidationCallback = (a, b, c, d) => true,
        });
        return services;
    }
}