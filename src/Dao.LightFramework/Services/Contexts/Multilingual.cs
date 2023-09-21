using Dao.LightFramework.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dao.LightFramework.Services.Contexts;

public static class MultilingualSetting
{
    public static Dictionary<string, string> LanguagePaths { get; set; } = new()
    {
        { "zh-cn", "i18n/zh-cn.json" },
        { "en-us", "i18n/en-us.json" }
    };
}

public class Multilingual : IMultilingual
{
    static readonly Dictionary<string, JObject> languages;

    static Multilingual() => languages = MultilingualSetting.LanguagePaths?.ToDictionary(kv => kv.Key, kv => (JObject)JsonConvert.DeserializeObject(File.ReadAllText(kv.Value)), StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

    readonly IRequestContext requestContext;

    public Multilingual(IRequestContext requestContext) => this.requestContext = requestContext;

    public string Get(ICollection<string> keys)
    {
        if (keys.IsNullOrEmpty())
            return string.Empty;

        var defaultValue = keys.Last();

        JObject json = null;
        var i = -1;
        foreach (var key in keys)
        {
            i++;

            if (i == 0)
                json = languages.GetValueOrDefault(this.requestContext.Language);
            if (json == null)
                return defaultValue;

            var item = json.GetValue(key);
            if (item == null)
                return defaultValue;

            if (i == keys.Count - 1)
                return item.ToString();
            json = item as JObject;
        }

        return defaultValue;
    }

    public string Get(ICollection<string> keys, params object[] args) => string.Format(Get(keys), args);

    public string Get(string key, params object[] args) => Get(new[] { key }, args);

    public string GetByLocale(string locale = null) => languages.GetValueOrDefault(locale ?? this.requestContext.Language).ToString(Formatting.None);

    public string GetAll() => JsonConvert.SerializeObject(languages);
}