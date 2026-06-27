using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser>  _userManager;

        public AuthController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { success = false, message = "Username and password are required." });

            var user = await _userManager.FindByNameAsync(req.Username);
            if (user == null)
                return Unauthorized(new { success = false, message = "Invalid username or password." });

            var result = await _signInManager.PasswordSignInAsync(user, req.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
                return Ok(new { success = true, username = user.UserName });

            return Unauthorized(new { success = false, message = "Invalid username or password." });
        }

        // POST api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { success = true });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
