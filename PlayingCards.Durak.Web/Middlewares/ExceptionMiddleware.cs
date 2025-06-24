using System.Net;

namespace PlayingCards.Durak.Web.Middlewares;

public class ExceptionMiddleware(RequestDelegate next)
{
    // IMessageWriter is injected into InvokeAsync
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (BusinessException ex)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await httpContext.Response.WriteAsync(ex.Message);
        }
    }
}
