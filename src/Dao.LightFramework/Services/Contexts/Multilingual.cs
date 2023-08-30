﻿using Dao.LightFramework.Common.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dao.LightFramework.Services.Contexts;

public class Multilingual : IMultilingual
{
    static readonly Dictionary<string, Lazy<JObject>> languages = new(StringComparer.OrdinalIgnoreCase)
    {
        { "zh-cn", new Lazy<JObject>(() => (JObject)JsonConvert.DeserializeObject(File.ReadAllText("i18n/zh-cn.json"))) },
        { "en-us", new Lazy<JObject>(() => (JObject)JsonConvert.DeserializeObject(File.ReadAllText("i18n/en-us.json"))) }
    };

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
                json = languages[this.requestContext.Language]?.Value;
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

    public string GetByLocale(string locale = null) => languages[locale ?? this.requestContext.Language]?.Value.ToString(Formatting.None);

    public string GetAll() => JsonConvert.SerializeObject(new Dictionary<string, JObject>
    {
        { "zh-cn", languages["zh-cn"]?.Value },
        { "en-us", languages["en-us"]?.Value }
    });
}