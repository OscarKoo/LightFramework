using System.Collections.Concurrent;
using System.Reflection;
using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dao.LightFramework.HttpApi.Utilities;

public class JsonIgnoreContractResolver : CamelCasePropertyNamesContractResolver
{
    static readonly ConcurrentDictionary<Tuple<Type, string>, MethodInfo> methods = new ();

    static MethodInfo GetShouldSerializeMethod(Type type, string name) => methods.GetOrAdd(new Tuple<Type, string>(type, name), k =>
    {
        var method = k.Item1.GetMethod("ShouldSerialize" + k.Item2, Type.EmptyTypes);
        return method != null && method.ReturnType == typeof(bool) ? method : null;
    });

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        var type = member.DeclaringType;

        if (property.ShouldSerialize == null
            && (typeof(IDomainSite).IsAssignableFrom(type) && member.Name.In(nameof(IDomainSite.Domain), nameof(IDomainSite.Site))
                || typeof(IMutable).IsAssignableFrom(type) && member.Name.In(nameof(IMutable.CreateUser), nameof(IMutable.CreateTime), nameof(IMutable.UpdateUser), nameof(IMutable.UpdateTime))))
        {
            property.ShouldSerialize = instance =>
            {
                if (instance == null)
                    return false;

                var method = GetShouldSerializeMethod(instance.GetType(), member.Name);
                return method != null && Convert.ToBoolean(method.Invoke(instance, null));
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