namespace WebApiApp.Services
{
    using Microsoft.EntityFrameworkCore;
    using WebApiApp.Models;
    using System.Threading.Tasks;
    using WebApiApp.Data;

    public class UserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null || !VerifyPasswordHash(password, user.PasswordHash))
            {
                return null;
            }

            return user;

        }

        public async Task<User> RegisterAsync(string name, string email, string password)
        {
            if (await _context.Users.AnyAsync(u=> u.Email == email))
            {
                throw new Exception("User with this email already exists.");
            }

            var passwordHash = CreatePasswordHash(password);
            var newUser = new User
            {
                Name = name,
                Email = email,
                PasswordHash = passwordHash
            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return newUser;
        }

        private string CreatePasswordHash(string password)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256())
            {
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hash);
            }
            
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256())
            {
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return storedHash == Convert.ToBase64String(hash);
            }
        }

    }
}
