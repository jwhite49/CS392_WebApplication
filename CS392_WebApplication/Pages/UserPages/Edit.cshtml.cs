using CS392_WebApplication.Models;
using CS392_WebApplication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CS392_WebApplication.Pages.UserPages
{
	[Authorize(Roles = "Admin")]
	public class EditModel : PageModel
	{
		private readonly UserDbContext _context;
		private readonly UserManager<IdentityUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly SystemLogDbContext _logContext;

		public EditModel(
			UserDbContext context, 
			UserManager<IdentityUser> userManager, 
			RoleManager<IdentityRole> roleManager,
			SystemLogDbContext logContext)
		{
			_context = context;
			_userManager = userManager;
			_roleManager = roleManager;
			_logContext = logContext;
		}

		[BindProperty]
		public new Models.User User { get; set; } = default!;

		[BindProperty]
		public string SelectedRole { get; set; }

		public string CurrentRole { get; set; }
		public List<string> AvailableRoles { get; set; } = new();

		public async Task<IActionResult> OnGetAsync(int? id)
		{
			if (id == null) return NotFound();

			var user = await _context.User.FirstOrDefaultAsync(m => m.UserID == id);
			if (user == null) return NotFound();

			User = user;

			// Load all available roles
			AvailableRoles = _roleManager.Roles.Select(r => r.Name).ToList();

			// Find the matching Identity user by email to get their current role
			var identityUser = await _userManager.FindByEmailAsync(user.Email);
			if (identityUser != null)
			{
				var roles = await _userManager.GetRolesAsync(identityUser);
				CurrentRole = roles.FirstOrDefault() ?? "No role assigned";
				SelectedRole = CurrentRole;
			}

			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid) return Page();

			// Get the currently logged-in admin user (use base.User to access ClaimsPrincipal)
			var adminUser = await _userManager.GetUserAsync(base.User);
			var originalUser = await _context.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == User.UserID);

			// Track what changed
			var changes = new List<string>();
			if (originalUser.FirstName != User.FirstName)
				changes.Add($"FirstName: '{originalUser.FirstName}' → '{User.FirstName}'");
			if (originalUser.LastName != User.LastName)
				changes.Add($"LastName: '{originalUser.LastName}' → '{User.LastName}'");
			if (originalUser.Email != User.Email)
				changes.Add($"Email: '{originalUser.Email}' → '{User.Email}'");
			if (originalUser.Username != User.Username)
				changes.Add($"Username: '{originalUser.Username}' → '{User.Username}'");

			// Save user table changes
			_context.Attach(User).State = EntityState.Modified;
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!UserExists(User.UserID)) return NotFound();
				else throw;
			}

			// Update the role in Identity
			var identityUser = await _userManager.FindByEmailAsync(User.Email);
			if (identityUser != null && !string.IsNullOrEmpty(SelectedRole))
			{
				// Remove all current roles then assign the new one
				var currentRoles = await _userManager.GetRolesAsync(identityUser);
				var oldRole = currentRoles.FirstOrDefault() ?? "None";

				if (oldRole != SelectedRole)
				{
					await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
					await _userManager.AddToRoleAsync(identityUser, SelectedRole);
					changes.Add($"Role: '{oldRole}' → '{SelectedRole}'");
				}

				// Log changes if any were made
				if (changes.Any())
				{
					var changeLog = new SystemLog
					{
						Timestamp = DateTime.UtcNow,
						Level = "Information",
						EventType = "UserProfileChange",
						Message = $"User profile updated for {User.Email} by admin",
						UserId = adminUser?.Id,
						TargetUserId = identityUser.Id,
						Page = "/UserPages/Edit",
						AdditionalData = $"{{ \"targetEmail\": \"{User.Email}\", \"changes\": [\"{string.Join("\", \"", changes)}\"] }}"
					};
					_logContext.SystemLog.Add(changeLog);
					await _logContext.SaveChangesAsync();
				}
			}

			return RedirectToPage("./Index");
		}

		private bool UserExists(int id)
		{
			return _context.User.Any(e => e.UserID == id);
		}
	}
}