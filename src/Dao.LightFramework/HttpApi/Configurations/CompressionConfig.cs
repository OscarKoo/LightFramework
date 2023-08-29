using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class CompressionConfig
{
    public static IServiceCollection AddLightResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
            o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml" });
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
        return services;
    }
}