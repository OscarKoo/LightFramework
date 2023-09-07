using Dao.LightFramework.Domain.Entities;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;

namespace Dao.LightFramework.Services.Contexts;

public class RequestContext : IRequestContext
{
    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        Initialize(httpContextAccessor);
    }

    public string Domain { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Language { get; set; } = "zh-cn";
    public string Token { get; set; }

    void Initialize(IHttpContextAccessor accessor)
    {
        var context = accessor?.HttpContext;
        if (context == null)
            return;

        var claims = context.User;
        if (claims.Identity?.IsAuthenticated ?? false)
        {
            UserId = claims.GetClaim("sub") ?? string.Empty;
            User = claims.GetClaim("username") ?? claims.GetClaim("name") ?? string.Empty;
        }

        var request = context.Request;
        var auth = request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth))
        {
            var auths = auth.Split(' ');
            Token = auths.Length >= 2 ? auths[1] : string.Empty;
        }

        var cookies = request.Cookies;
        if (cookies.TryGetValue("lang", out var lang)
            && !string.IsNullOrWhiteSpace(lang))
            Language = lang;
    }

    public void FillEntity<T>(T entity)
    {
        if (entity is IDomainSite ds)
        {
            ds.Domain ??= Domain;
            ds.Site ??= Site;
        }

        if (entity is IMutable mutable)
        {
            mutable.CreateUser ??= UserId;
            mutable.CreateTime ??= DateTime.Now;

            mutable.UpdateUser = UserId;
            mutable.UpdateTime = DateTime.Now;
        }
    }
}