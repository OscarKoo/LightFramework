using System.Net;
using Dao.LightFramework.Common.Exceptions;
using Dao.LightFramework.Common.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dao.LightFramework.HttpApi.Filters;

public class ExceptionHandler : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is ConfirmException confirm)
        {
            confirm.Write(context);
            context.ExceptionHandled = true;
            return;
        }

        var isWarning = context.Exception is WarningException;
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.StatusCode = context.Exception is BadHttpRequestException { StatusCode: > 0 } bhrEx
            ? bhrEx.StatusCode
            : isWarning
                ? (int)HttpStatusCode.BadRequest
                : (int)HttpStatusCode.InternalServerError;

        var result = new ExceptionResult
        {
            Type = (isWarning ? ExceptionType.Warning : ExceptionType.Error).ToString(),
            Message = context.Exception.GetBaseException().Message
        };
        if (!isWarning)
            StaticLogger.LogError(context.Exception, result.Message);

        context.Result = new JsonResult(result);
        context.ExceptionHandled = true;
    }
}