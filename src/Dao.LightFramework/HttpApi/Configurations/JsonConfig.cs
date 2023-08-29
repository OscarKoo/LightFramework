using Dao.LightFramework.HttpApi.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Dao.LightFramework.HttpApi.Configurations;

public static class JsonConfig
{
    public static IMvcBuilder AddLightJson(this IMvcBuilder builder)
    {
        builder.AddNewtonsoftJson(o =>
        {
            o.SerializerSettings.ContractResolver = new JsonIgnoreContractResolver();
            o.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            o.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        });
        return builder;
    }
}