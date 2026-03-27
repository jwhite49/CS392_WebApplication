using CS392_WebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages.UserPages
{
	[Authorize(Roles = "Admin")]
	public class IndexModel : PageModel
	{
		private readonly UserDbContext _context;
		private readonly UserManager<IdentityUser> _userManager;

		public IndexModel(UserDbContext context, UserManager<IdentityUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public IList<User> User { get; set; } = default!;

		// ✅ Dictionary to map email -> role
		public Dictionary<string, string> UserRoles { get; set; } = new();

		public async Task OnGetAsync()
		{
			User = await _context.User.ToListAsync();

			// ✅ For each user, look up their Identity role by email
			foreach (var user in User)
			{
				var identityUser = await _userManager.FindByEmailAsync(user.Email);
				if (identityUser != null)
				{
					var roles = await _userManager.GetRolesAsync(identityUser);
					UserRoles[user.Email] = roles.FirstOrDefault() ?? "No role";
				}
				else
				{
					UserRoles[user.Email] = "Not in Identity";
				}
			}
		}
	}
}