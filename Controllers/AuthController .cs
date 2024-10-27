using Microsoft.AspNetCore.Mvc;

using WebApiApp.Models;
using WebApiApp.Services;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using WebApiApp.Exceptions;

namespace WebApiApp.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class AuthController(AuthService authService) : ControllerBase
    {

        private readonly AuthService _authService = authService;

        //auth
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginModel login)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _authService.AuthenticateAsync(login.Email, login.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }
            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Пользователь заблокирован." });
            }

            if (user.IsEnable2fa)
            {
                var otpValidationToken =_authService.GenerateOtpValidationTokenAsync();
                await _authService.SetOtpValidationTokenAsync(user, otpValidationToken);
                return Ok(new { message = "2FA required.", otpValidationToken = user.OtpValidationToken });
            }
            await _authService.SignInAsync(user);

            return Ok(new { message = "Logged in successfully." });
           
        }

        // Логаут пользователя
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully." });
        }

        //registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel register)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var user = await _authService.RegisterAsync(register.Name, register.Email, register.Password);
                
                return Ok(new { message = "Registered successfully." });
            }
            catch (UserAlreadyExistsException)
            {
                return BadRequest(new { message = "User with this email already exists." });
            }
            catch (Exception ex) // Ловим другие возможные ошибки
            {
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        // Подтверждение почты
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            var result = await _authService.ConfirmEmailAsync(token);
            if (result)
            {
                return Ok("Email confirmed successfully.");
            }
            return BadRequest(result);
        }

        // Активация 2FA
        [HttpPost("enable-2fa")]
        public async Task<IActionResult> Enable2FA()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user == null) return Unauthorized("User is not authenticated.");

            await _authService.Activate2FAAsync(user);
            var uri = await _authService.GenerateQrCodeUri(user);

            return Ok(new { qrCodeUri = uri });
        }

        // Вход с 2FA
        [HttpPost("/otp/verify")]
        public async Task<IActionResult> Login(string otpValidationToken, string otpCode)
        {
            var user = await _authService.AuthenticateWith2FAAsync(otpValidationToken, otpCode);
            if (user == null) return Unauthorized("Invalid email, password, or OTP code.");

            await _authService.SignInAsync(user);
            return Ok("Logged in successfully.");
        }


    }

    
}
