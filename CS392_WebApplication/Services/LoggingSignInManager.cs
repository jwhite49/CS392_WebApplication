using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using CS392_WebApplication.Data;
using CS392_WebApplication.Models;

public class LoggingSignInManager : SignInManager<IdentityUser>
{
    private readonly SystemLogDbContext _logContext;

    public LoggingSignInManager(
        UserManager<IdentityUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<IdentityUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<IdentityUser> confirmation,
        SystemLogDbContext logContext)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _logContext = logContext;
    }

    public override async Task<SignInResult> PasswordSignInAsync(
        string userName,
        string password,
        bool isPersistent,
        bool lockoutOnFailure)
    {
        var result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);

        if (!result.Succeeded)
        {
            var user = await UserManager.FindByNameAsync(userName);

            var log = new SystemLog
            {
                Timestamp = DateTime.UtcNow,
                Level = "Warning",
                EventType = "FailedLogin",
                Message = $"Failed login attempt for username '{userName}'",
                UserId = user?.Id,
                TargetUserId = user?.Id,
                Page = "/Identity/Account/Login",
                AdditionalData = $"{{ \"usernameAttempted\": \"{userName}\" }}"
            };

            _logContext.SystemLog.Add(log);
            await _logContext.SaveChangesAsync();
        }

        return result;
    }
}
