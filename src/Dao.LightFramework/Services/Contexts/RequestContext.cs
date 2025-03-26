using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Dao.LightFramework.Services.Contexts;

public class RequestContext : IRequestContext
{
    protected readonly IHttpContextAccessor httpContextAccessor;

    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
        Initialize(httpContextAccessor);
    }

    public string Domain { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Operator { get; set; }
    public string Language { get; set; } = "zh-cn";
    public string Token { get; set; }

    void Initialize(IHttpContextAccessor accessor)
    {
        var ctx = RequestContextInfo.Context;
        if (ctx != null)
        {
            Domain = ctx.Domain;
            Site = ctx.Site;
            UserId = ctx.UserId;
            User = ctx.User;
            Operator = ctx.Operator;
            Language = ctx.Language;
            Token = ctx.Token;
            RequestContextInfo.Context = this;
            return;
        }

        var context = accessor?.HttpContext;
        if (context == null)
            return;

        string lang = null;
        var claims = this.Caught(() => context.User); // unknown null reference exception thrown here ?!
        if (claims?.Identity?.IsAuthenticated ?? false)
        {
            UserId = claims.GetClaim(Claims.Subject) ?? string.Empty;
            User = claims.GetClaim(Claims.Username) ?? claims.GetClaim(Claims.Name) ?? string.Empty;
            Operator = claims.GetClaim("operator");
            Domain = claims.GetClaim("domain") ?? string.Empty;
            Site = claims.GetClaim("site") ?? string.Empty;
            lang = claims.GetClaim(nameof(lang));
            if (!string.IsNullOrWhiteSpace(lang))
                Language = lang;
        }

        var request = context.Request;
        var auth = request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(auth))
        {
            var auths = auth.Split(' ');
            Token = auths.Length >= 2 ? auths[1] : string.Empty;
        }

        if (string.IsNullOrWhiteSpace(lang))
        {
            var cookies = request.Cookies;
            if (cookies.TryGetValue(nameof(lang), out lang) && !string.IsNullOrWhiteSpace(lang))
                Language = lang;
        }
    }

    public void Reinitialize() => Initialize(this.httpContextAccessor);

    public void FillEntity<T>(T entity)
    {
        if (entity is IDomainSite ds)
        {
            ds.Domain ??= Domain;
            ds.Site ??= Site;
        }

        if (entity is IMutable mutable)
        {
            mutable.CreateUser ??= Operator ?? UserId;
            mutable.CreateTime ??= DateTime.Now;

            mutable.UpdateUser = Operator ?? UserId;
            mutable.UpdateTime = DateTime.Now;
        }
    }
}