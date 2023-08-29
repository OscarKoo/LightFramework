using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.HttpApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class AuthorizeConfig
{
    public static IServiceCollection AddLightAuthorize(this IServiceCollection services)
    {
        var typeExtender = new TypeExtender(nameof(AppController));
        typeExtender.AddAttribute<AuthorizeAttribute>();
        return services;
    }
}