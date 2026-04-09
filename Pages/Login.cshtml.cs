using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DocManagerAI.Data;
using DocManagerAI.Services;

namespace DocManagerAI.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;
        public LoginModel(AppDbContext db) => _db = db;

        [BindProperty] public string Username { get; set; }
        [BindProperty] public string Password { get; set; }
        public string Message { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                Message = "Please enter both username and password.";
                return Page();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == Username);

            if (user != null && AuthService.Verify(Password, user.PasswordHash))
            {
                HttpContext.Session.SetString("User", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetInt32("UserId", user.Id);
                return RedirectToPage("/Index");
            }

            Message = "Invalid username or password. Please try again.";
            return Page();
        }
    }
}
