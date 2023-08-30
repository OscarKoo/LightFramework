using Dao.LightFramework.Common.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class ReadRequestBodyConfig
{
    public static IServiceCollection EnableReadRequestBody(this IServiceCollection services, bool enable = true)
    {
        ReadRequestBodyAttribute.Enabled = enable;
        return services;
    }
}