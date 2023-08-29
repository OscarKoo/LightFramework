using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.HttpApi.Filters;
using Microsoft.AspNetCore.Builder;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class MiddlewareConfig
{
    public static IApplicationBuilder AddLightMiddleware(this IApplicationBuilder app)
    {
        if (ReadRequestBodyAttribute.Enabled)
            app.UseMiddleware<RequestMiddleware>();
        return app;
    }
}