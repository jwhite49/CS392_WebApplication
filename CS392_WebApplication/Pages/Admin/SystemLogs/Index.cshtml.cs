using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CS392_WebApplication.Data;
using CS392_WebApplication.Models;

namespace CS392_WebApplication.Pages.Admin.SystemLogs
{
    public class IndexModel : PageModel
    {
        private readonly SystemLogDbContext _context;

        public IndexModel(SystemLogDbContext context)
        {
            _context = context;
        }

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? Level { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EventType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TargetUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PageFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public List<SystemLog> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _context.SystemLog.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(Level))
                query = query.Where(l => l.Level == Level);

            if (!string.IsNullOrEmpty(EventType))
                query = query.Where(l => l.EventType == EventType);

            if (!string.IsNullOrEmpty(UserId))
                query = query.Where(l => l.UserId == UserId);

            if (!string.IsNullOrEmpty(TargetUserId))
                query = query.Where(l => l.TargetUserId == TargetUserId);

            if (!string.IsNullOrEmpty(PageFilter))
                query = query.Where(l => l.Page.Contains(PageFilter));

            if (StartDate.HasValue)
                query = query.Where(l => l.Timestamp >= StartDate.Value);

            if (EndDate.HasValue)
                query = query.Where(l => l.Timestamp <= EndDate.Value);

            Logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(200) // prevent huge loads
                .ToListAsync();
        }

    }
}
