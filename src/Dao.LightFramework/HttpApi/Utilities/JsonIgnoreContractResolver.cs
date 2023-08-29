using System.Reflection;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dao.LightFramework.HttpApi.Utilities;

public class JsonIgnoreContractResolver : CamelCasePropertyNamesContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        var type = member.DeclaringType;

        if (typeof(IDomainSite).IsAssignableFrom(type))
        {
            property.ShouldSerialize = member.Name switch
            {
                nameof(IDomainSite.Domain) => _ => false,
                nameof(IDomainSite.Site) => _ => false,
                _ => property.ShouldSerialize
            };
        }

        if (typeof(IMutable).IsAssignableFrom(type))
        {
            property.ShouldSerialize = member.Name switch
            {
                nameof(IMutable.CreateUser) => _ => false,
                nameof(IMutable.CreateTime) => _ => false,
                nameof(IMutable.UpdateUser) => _ => false,
                nameof(IMutable.UpdateTime) => _ => false,
                _ => property.ShouldSerialize
            };
        }

        if (property.PropertyType == typeof(TimeSpan))
        {
            property.Converter = new TimeSpanConverter();
        }

        return property;
    }
}

public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer) =>
        writer.WriteValue(value.ToString(@"hh\:mm"));

    public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer) =>
        ((string)reader.Value).ToTimeSpan();
}