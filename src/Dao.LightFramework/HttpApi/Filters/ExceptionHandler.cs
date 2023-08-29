using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Dao.LightFramework.Common.Exceptions;
using Dao.LightFramework.Common.Utilities;

namespace Dao.LightFramework.HttpApi.Filters;

public class ExceptionHandler : ExceptionFilterAttribute
{
    static readonly string TypeError = ExceptionType.Error.ToString();

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is ConfirmException confirm)
        {
            confirm.Write(context);
            context.ExceptionHandled = true;
            return;
        }

        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var result = new
        {
            Type = (context.Exception is WarningException ? ExceptionType.Warning : ExceptionType.Error).ToString(),
            context.Exception.GetBaseException().Message
        };
        if (result.Type == TypeError)
            StaticLogger.LogError(context.Exception, result.Message);

        context.Result = new JsonResult(result);
        context.ExceptionHandled = true;
    }
}