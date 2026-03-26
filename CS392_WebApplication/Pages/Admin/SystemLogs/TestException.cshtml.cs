using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CS392_WebApplication.Pages.Admin.SystemLogs
{
    public class TestExceptionModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            throw new Exception("This is a test exception triggered manually.");
        }
    }
}
