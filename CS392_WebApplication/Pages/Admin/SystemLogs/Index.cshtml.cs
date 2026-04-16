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
        private static readonly TimeZoneInfo EasternZone =
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

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
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 25;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public List<SystemLog> Logs { get; set; } = new();

        public async Task OnGetAsync()
        {
            var query = _context.SystemLog.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(Level))
                query = query.Where(l => l.Level == Level);

            if (!string.IsNullOrEmpty(EventType))
                query = query.Where(l => l.EventType == EventType);

            // Enhanced search - searches across multiple fields
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(l => 
                    l.Message.ToLower().Contains(searchLower) ||
                    (l.UserId != null && l.UserId.ToLower().Contains(searchLower)) ||
                    (l.TargetUserId != null && l.TargetUserId.ToLower().Contains(searchLower)) ||
                    (l.Page != null && l.Page.ToLower().Contains(searchLower)) ||
                    (l.AdditionalData != null && l.AdditionalData.ToLower().Contains(searchLower))
                );
            }

            if (StartDate.HasValue)
                query = query.Where(l => l.Timestamp >= StartDate.Value);

            if (EndDate.HasValue)
                query = query.Where(l => l.Timestamp <= EndDate.Value);

            TotalCount = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            Logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Converts a UTC timestamp to US Eastern Time for display.
        /// </summary>
        public string ToEasternTime(DateTime utcTimestamp)
        {
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, EasternZone);
            return eastern.ToString("MM/dd/yyyy hh:mm:ss tt") + " ET";
        }
    }
}
