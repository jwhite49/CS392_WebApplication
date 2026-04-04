using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserDbContext _userContext;
        private readonly School_UserDbContext _schoolUserContext;
        private readonly UserManager<IdentityUser> _userManager;

        public bool HasExistingRequest { get; set; }
        public bool IsAlreadySchool { get; set; }

        public IndexModel(ILogger<IndexModel> logger, UserDbContext userContext, School_UserDbContext schoolUserContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userContext = userContext;
            _schoolUserContext = schoolUserContext;
            _userManager = userManager;
        }

        public async Task OnGetAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                IsAlreadySchool = User.IsInRole("School");

                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser != null)
                {
                    var dbUser = await _userContext.User.FirstOrDefaultAsync(u => u.Email == identityUser.Email);
                    if (dbUser != null)
                    {
                        HasExistingRequest = await _schoolUserContext.School_User.AnyAsync(s => s.userID == dbUser.UserID);
                    }
                }
            }
        }
    }
}
