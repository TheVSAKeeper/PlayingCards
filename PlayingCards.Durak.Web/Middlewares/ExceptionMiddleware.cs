using System.Net;

namespace PlayingCards.Durak.Web.Middlewares;

public class ExceptionMiddleware(ILogger<ExceptionMiddleware> logger, RequestDelegate next)
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
            logger.LogError(ex, "Business exception: {Message}", ex.Message);
            httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await httpContext.Response.WriteAsync(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception: {Message}", ex.Message);
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
}
