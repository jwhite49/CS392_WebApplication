using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

            // Store error details in TempData so the error page can display them
            var tempDataFactory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = tempDataFactory.GetTempData(context);
            tempData["ErrorMessage"] = ex.Message;
            tempData["ErrorDetails"] = ex.ToString();
            tempData.Save();

            // Redirect to custom error page
            context.Response.Redirect("/Error");
        }
    }
}
