using System.Diagnostics;
using System.Reflection;
using Dao.LightFramework.Common.Attributes;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Dao.LightFramework.HttpApi.Filters;

public class SwaggerIgnoreFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema == null || context == null)
            return;

        Debug.WriteLine($"======== Apply: {context.MemberInfo?.Name}: {context.Type.Name}");

        if (context.Type == typeof(TimeSpan))
        {
            schema.Type = "string";
            schema.Example = new OpenApiString("00:00");
        }

        if (context.MemberInfo != null)
        {
            if (context.MemberInfo.HasAttribute<SwaggerIgnoreAttribute>() || context.MemberInfo.DeclaringType == typeof(TimeSpan))
            {
                schema.ReadOnly = true;
                schema.WriteOnly = true;
            }

            if (context.MemberInfo is PropertyInfo pi && pi.PropertyType == typeof(string))
            {
                schema.MaxLength = 36;
            }

            if (context.MemberInfo.Name.EndsWith(nameof(IId.Id), StringComparison.Ordinal))
            {
                schema.Type = "string";
                schema.Format = "uuid";
            }
            else if (context.MemberInfo.Name.EndsWith(nameof(IRowVersion.RowVersion), StringComparison.Ordinal))
            {
                schema.Example = new OpenApiString(null);
            }
        }

        if (context.ParameterInfo != null)
        {
            if (context.ParameterInfo.ParameterType == typeof(string))
            {
                schema.MaxLength = 36;
            }

            if (context.ParameterInfo.Name?.EndsWith(nameof(IId.Id), StringComparison.Ordinal) ?? false)
            {
                schema.Type = "string";
                schema.Format = "uuid";
            }
        }
    }
}