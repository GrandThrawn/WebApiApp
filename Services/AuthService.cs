namespace WebApiApp.Services
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using System.Security.Claims;
    using WebApiApp.Models;

    public class AuthService
    {
        private readonly UserService _userService;
        private readonly HttpContext _httpContext;

        public AuthService(UserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public async Task SignInAsync(User user, bool isPersistent = true)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            await _httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            return await _userService.AuthenticateAsync(email,password);
        }

        public async Task<User> RegisterAsync(string name, string email, string password)
        {
            return await _userService.RegisterAsync(name, email, password);
        }

    }
}
