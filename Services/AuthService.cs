namespace WebApiApp.Services
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using System.Security.Claims;
    using WebApiApp.Models;
    using System;
    using System.Threading.Tasks;
    using WebApiApp.Exceptions;
    using Microsoft.AspNetCore.Identity;
    using OtpNet;
    using Microsoft.EntityFrameworkCore;

    public class AuthService(UserService userService, IHttpContextAccessor httpContextAccessor)
    {
        private readonly UserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        private readonly HttpContext _httpContext = httpContextAccessor.HttpContext ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        public async Task SignInAsync(User user, int expirationMinutes = 60)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
            };

            await _httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            var user = await _userService.AuthenticateAsync(email, password);
            if (user == null)
            {
                return null;
            }
            return user;
        }

        
        public bool ValidateOtpCode(string secret, string otpCode)
        {
            var otp = new Totp(Base32Encoding.ToBytes(secret));
            return otp.VerifyTotp(otpCode, out long timeStepMatched, new VerificationWindow(2, 2));
        }

        public async Task<User> RegisterAsync(string name, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException(null, nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException(null, nameof(password));

            var existingUser = await _userService.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                throw new UserAlreadyExistsException("User with this email already exists.");
            }
            var newUser = await _userService.RegisterAsync(name, email, password);
            return newUser;
                       
        }
        public async Task<bool> ConfirmEmailAsync(string token)
        {
            return await _userService.ConfirmEmailAsync(token);
        }

        public async Task Activate2FAAsync(User user)
        {
            await _userService.Activate2FAAsync(user);
        }

        public async Task<string> GenerateQrCodeUri(User user)
        {
            return await _userService.GenerateQrCodeUri(user.Email, user.OtpSecret);
        }

        public async Task<User> AuthenticateWith2FAAsync(string otpValidationToken, string otpCode)
        {
            var user = await _userService.GetUserByOtpValidationTokenAsync(otpValidationToken);
            if (user == null || string.IsNullOrEmpty(user.OtpSecret) || !ValidateOtpCode(user.OtpSecret, otpCode))
            {
                return null;
            }
            await _userService.ClearOtpValidationTokenAsync(user);
            return user;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var email = _httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            return await _userService.GetUserByEmailAsync(email);
        }

        public string GenerateOtpValidationTokenAsync()
        {
            var otpToken = Guid.NewGuid().ToString();            

            return otpToken;
        }

        public async Task SetOtpValidationTokenAsync(User user, string otpValidationToken)
        {
            await _userService.SetOtpValidationTokenAsync(user, otpValidationToken);           
        }
    }
}
