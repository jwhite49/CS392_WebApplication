using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CS392_WebApplication.Models;
using CS392_WebApplication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages.SchoolUserPages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly School_UserDbContext _context;
        private readonly UserDbContext _userContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SystemLogDbContext _logContext;

        public List<SchoolRequestViewModel> SchoolRequests { get; set; } = new();

        public IndexModel(
            School_UserDbContext context, 
            UserDbContext userContext, 
            UserManager<IdentityUser> userManager,
            SystemLogDbContext logContext)
        {
            _context = context;
            _userContext = userContext;
            _userManager = userManager;
            _logContext = logContext;
        }

        public async Task OnGetAsync()
        {
            var schoolUsers = await _context.School_User.ToListAsync();
            var users = await _userContext.User.ToListAsync();

            SchoolRequests = schoolUsers.Select(su => new SchoolRequestViewModel
            {
                SchoolUser = su,
                User = users.FirstOrDefault(u => u.UserID == su.userID)
            }).ToList();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            var schoolUser = await _context.School_User.FindAsync(id);
            if (schoolUser == null) return NotFound();

            schoolUser.is_approved = true;
            await _context.SaveChangesAsync();

            var dbUser = await _userContext.User.FindAsync(id);
            if (dbUser != null)
            {
                var identityUser = await _userManager.FindByEmailAsync(dbUser.Email);
                if (identityUser != null)
                {
                    var currentRoles = await _userManager.GetRolesAsync(identityUser);
                    var oldRoles = string.Join(", ", currentRoles);
                    
                    await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                    await _userManager.AddToRoleAsync(identityUser, "School");

                    // Log school account approval
                    var approvalLog = new SystemLog
                    {
                        Timestamp = DateTime.UtcNow,
                        Level = "Information",
                        EventType = "SchoolAccountApproval",
                        Message = $"School account approved for {dbUser.Email} ({dbUser.FirstName} {dbUser.LastName})",
                        UserId = adminUser?.Id,
                        TargetUserId = identityUser.Id,
                        Page = "/SchoolUserPages/Index",
                        AdditionalData = $"{{ \"targetEmail\": \"{dbUser.Email}\", \"schoolName\": \"{schoolUser.school_name}\", \"previousRoles\": \"{oldRoles}\", \"newRole\": \"School\" }}"
                    };
                    _logContext.SystemLog.Add(approvalLog);
                    await _logContext.SaveChangesAsync();
                }
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDenyAsync(int id)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            var schoolUser = await _context.School_User.FindAsync(id);
            if (schoolUser != null)
            {
                var dbUser = await _userContext.User.FindAsync(id);
                var userEmail = dbUser?.Email ?? "Unknown";

                _context.School_User.Remove(schoolUser);
                await _context.SaveChangesAsync();

                // Log school account denial
                var denialLog = new SystemLog
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    EventType = "SchoolAccountDenial",
                    Message = $"School account request denied for {userEmail}",
                    UserId = adminUser?.Id,
                    TargetUserId = null,
                    Page = "/SchoolUserPages/Index",
                    AdditionalData = $"{{ \"targetEmail\": \"{userEmail}\", \"schoolName\": \"{schoolUser.school_name}\" }}"
                };
                _logContext.SystemLog.Add(denialLog);
                await _logContext.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }

    public class SchoolRequestViewModel
    {
        public School_User SchoolUser { get; set; }
        public User User { get; set; }
    }
}
