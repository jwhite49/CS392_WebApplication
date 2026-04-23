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
        var user = await UserManager.FindByNameAsync(userName);

        if (result.Succeeded)
        {
            // Log successful login
            var successLog = new SystemLog
            {
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                EventType = "SuccessfulLogin",
                Message = $"User '{userName}' successfully logged in",
                UserId = user?.Id,
                TargetUserId = user?.Id,
                Page = "/Identity/Account/Login",
                AdditionalData = $"{{ \"username\": \"{userName}\", \"isPersistent\": {isPersistent.ToString().ToLower()} }}"
            };

            _logContext.SystemLog.Add(successLog);
            await _logContext.SaveChangesAsync();
        }
        else if (!result.Succeeded)
        {
            // Log failed login
            var failLog = new SystemLog
            {
                Timestamp = DateTime.UtcNow,
                Level = "Warning",
                EventType = "FailedLogin",
                Message = $"Failed login attempt for username '{userName}'",
                UserId = user?.Id,
                TargetUserId = user?.Id,
                Page = "/Identity/Account/Login",
                AdditionalData = $"{{ \"usernameAttempted\": \"{userName}\", \"isLockedOut\": {result.IsLockedOut.ToString().ToLower()}, \"requiresTwoFactor\": {result.RequiresTwoFactor.ToString().ToLower()} }}"
            };

            _logContext.SystemLog.Add(failLog);
            await _logContext.SaveChangesAsync();
        }

        return result;
    }
}
