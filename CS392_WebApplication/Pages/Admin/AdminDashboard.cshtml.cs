using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CS392_WebApplication.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardModel : PageModel
    {
        private readonly ILogger<AdminDashboardModel> _logger;

        public AdminDashboardModel(ILogger<AdminDashboardModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Admin accessed the dashboard.");
            // You can load data here if needed, e.g. user stats, appointment summaries, etc.
        }
    }
}