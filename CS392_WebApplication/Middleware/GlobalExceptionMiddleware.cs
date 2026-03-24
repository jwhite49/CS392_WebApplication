using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using CS392_WebApplication.Data;
using CS392_WebApplication.Models;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SystemLogDbContext logContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log exception to SystemLog table
            var log = new SystemLog
            {
                Timestamp = DateTime.UtcNow,
                Level = "Critical",
                EventType = "Exception",
                Message = ex.Message,
                StackTrace = ex.ToString(),
                UserId = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst("sub")?.Value
                    : null,
                Page = context.Request.Path,
                AdditionalData = "{ \"source\": \"GlobalExceptionMiddleware\" }"
            };

            logContext.SystemLog.Add(log);
            await logContext.SaveChangesAsync();

            // Redirect to custom error page
            context.Response.Redirect("/Error");
        }
    }
}
