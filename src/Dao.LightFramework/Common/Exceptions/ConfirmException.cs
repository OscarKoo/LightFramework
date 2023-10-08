using Dao.LightFramework.Common.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Dao.LightFramework.Common.Exceptions;

public class ConfirmException : Exception
{
    const string Header = "X-Confirm";

    public ConfirmException(ConfirmContent content, object data)
    {
        this.content = content;
        this.data = data;
    }

    readonly ConfirmContent content;
    readonly object data;

    public void Write(ExceptionContext context)
    {
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.Headers["Access-Control-Expose-Headers"] = Header;
        context.HttpContext.Response.Headers[Header] = Uri.EscapeDataString(JsonConvert.SerializeObject(this.content));
        context.HttpContext.Response.StatusCode = 600;
        context.Result = new JsonResult(new ExceptionResult
        {
            Type = ExceptionType.Confirm.ToString(),
            Message = this.content.Q,
            Data = this.data
        });
        context.ExceptionHandled = true;
    }

    static bool GetResult(HttpContext context, string key, string id, out ConfirmContent history, out ConfirmOption result)
    {
        var values = new StringValues();
        context?.Request.Headers.TryGetValue(Header, out values);
        var header = Uri.UnescapeDataString(values.ToString());

        ConfirmContent content;
        if (!string.IsNullOrWhiteSpace(header)
            && (content = JsonConvert.DeserializeObject<ConfirmContent>(header)) != null)
        {
            if (!string.IsNullOrWhiteSpace(content.A))
            {
                var current = content.Copy();
                content.Add(current);
            }

            history = content;
            return content.GetResult(key, id, out result);
        }

        history = null;
        result = ConfirmOption.None;
        return false;
    }

    public static ConfirmOption ConfirmedOrThrow(HttpContext context, string key, IEnumerable<string> ids,
        object data, string message, ConfirmOption options = ConfirmOption.No | ConfirmOption.Yes)
    {
        var id = string.Join("|", ids.OrderByIgnoreCase());

        if (GetResult(context, key, id, out var history, out var result) && result > ConfirmOption.No)
            return result;

        if (history == null)
        {
            history = new ConfirmContent();
            history.Reset(key, id, message, options);
        }
        else
        {
            if (history.Matched(key, id))
            {
                history.A = null;
            }
        }

        throw new ConfirmException(history, data);
    }
}

[Flags]
public enum ConfirmOption
{
    None = 0,
    No = 1,
    Yes = 2,
    ForceSchedule = 4
}

public class ConfirmHistory
{
    /// <summary>
    /// Key
    /// </summary>
    public string K { get; set; }
    /// <summary>
    /// Answer
    /// </summary>
    public string A { get; set; }
    /// <summary>
    /// Parameter
    /// </summary>
    public string P { get; set; }

    public bool Matched(string key, string id) => K.EqualsIgnoreCase(key) && P.EqualsIgnoreCase(id);

    public ConfirmHistory Copy() => new()
    {
        K = K,
        A = A,
        P = P
    };
}

public class ConfirmContent : ConfirmHistory
{
    /// <summary>
    /// Question
    /// </summary>
    public string Q { get; set; }

    /// <summary>
    /// Options
    /// </summary>
    public ConfirmOption O { get; set; }

    public List<ConfirmHistory> Histories { get; set; }

    public bool GetResult(string key, string id, out ConfirmOption result)
    {
        var history = Matched(key, id) ? this : Histories?.Find(w => w.Matched(key, id));
        if (history != null)
            return Enum.TryParse(history.A, out result);

        result = ConfirmOption.None;
        return false;
    }

    public void Add(ConfirmHistory history)
    {
        Histories ??= new List<ConfirmHistory>();
        Histories.Add(history);
    }

    public void Reset(string key, string id, string message, ConfirmOption options)
    {
        K = key;
        A = null;
        P = id;
        Q = message;
        O = options;
    }
}