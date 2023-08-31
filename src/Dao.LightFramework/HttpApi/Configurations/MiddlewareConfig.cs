using Dao.LightFramework.HttpApi.Filters;
using Microsoft.AspNetCore.Builder;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class MiddlewareConfig
{
    public static IApplicationBuilder AddLightMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestMiddleware>();
        return app;
    }
}