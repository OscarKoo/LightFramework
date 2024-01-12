using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class CompressionConfig
{
    public static IServiceCollection AddLightResponseCompression(this IServiceCollection services, CompressionLevel level = CompressionLevel.Optimal, params string[] mimeTypes)
    {
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
            o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "image/svg+xml" }).Concat(mimeTypes);
        });
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = level);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = level);
        return services;
    }
}