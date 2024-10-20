using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WebApiApp.Models;
using WebApiApp.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace WebApiApp.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class AuthController : ControllerBase
    {

        private readonly AuthService _authService;
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        //auth
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginModel login)
        {
            var user = await _authService.AuthenticateAsync(login.Email, login.Password);
            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }
            await _authService.SignInAsync(user);

            return Ok("Logged in successfully.");
           
        }

        // Логаут пользователя
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logged out successfully.");
        }

        //registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel register)
        {
            try
            {
                var user = await _authService.RegisterAsync(register.Name, register.Email, register.Password);
                await _authService.SignInAsync(user);
                return Ok("Registered and logged in successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
    }

    
}
