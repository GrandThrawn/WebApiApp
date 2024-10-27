namespace WebApiApp.Services
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;
    using System.Threading.Tasks;
    using System.Security.Cryptography;
    using System;
    using WebApiApp.Data;
    using WebApiApp.Exceptions;
    using WebApiApp.Models;
    using WebApiApp.Interfaces;
    using Microsoft.AspNetCore.DataProtection;
    
    public class UserService(AppDbContext context, IEmailSender emailSender, IConfiguration configuration)
    {
        private readonly AppDbContext _context = context;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly IConfiguration _configuration = configuration;

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return null;
            }
            return user;
        }

        public async Task<User?> GetUserByOtpValidationTokenAsync(string otpValidationToken)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.OtpValidationToken == otpValidationToken);
            if (user == null)
            {
                return null;
            }
                return user;
        }
        public async Task ClearOtpValidationTokenAsync(User user)
        {
            user.OtpValidationToken = null;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
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
                throw new UserAlreadyExistsException("User with this email already exists.");
            }

            var passwordHash = CreatePasswordHash(password);
            var newUser = new User
            {
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                RegisteredAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = false, // по умолчнию не актиыный пользователь
                IsVerified = false,
                MailValidationToken = GenerateMailValidationToken(),
                

            };
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            await SendEmailConfirmationAsync(newUser);

            return newUser;
        }

        private async Task SendEmailConfirmationAsync(User user)
        {
            string baseUrl = _configuration["App:BaseUrl"];
            string fromAddress = _configuration["Smtp:FromAddress"];
            var confirmationLink = $"{baseUrl}/auth/confirm-email?token={user.MailValidationToken}";
            var emailBody = $"В системе зарегестрировался новый пользователь {user.Name} Отправьте ему на почту {user.Email} ссылку на подтверждение: <a href='{confirmationLink}'>Confirm Email</a>";
            await _emailSender.SendEmailAsync(fromAddress, "Email Confirmation", emailBody);

        }

        private string GenerateMailValidationToken()
        {
            return Guid.NewGuid().ToString();
        }
        
        public async Task<bool> ConfirmEmailAsync(string token)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.MailValidationToken == token);
            if (user == null) return false;

            user.IsVerified = true;
            user.IsActive = true;
            user.MailValidationToken = null;
            await _context.SaveChangesAsync();
            return true;

        }


        public async Task<string> GenerateQrCodeUri(string email, string secret)
        {
            var issuer = _configuration["App:NameApp"];
            return $"otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}&digits=6";
        }

        public async Task Activate2FAAsync (User user)
        {
            if (user.IsEnable2fa)
            {
                throw new InvalidOperationException("2FA already enabled for this user.");
            }
            user.OtpSecret = GenerateOtpSecret();
            user.IsEnable2fa = true;
            await _context.SaveChangesAsync();
        }

        private static string GenerateOtpSecret()
        {
            byte[] secret = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {                
                rng.GetBytes(secret);               
            }
            return Base32Encode(secret);
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            int inByteSize = 8;
            int outByteSize = 5;
            string output = "";

            int bitsRemaining = 0;
            int buffer = 0;
            for (int i = 0; i< data.Length; i++)
            {
                buffer = (buffer << inByteSize) | data[i];
                bitsRemaining += inByteSize;

                while (bitsRemaining >= outByteSize)
                {
                    int index = (buffer >> (bitsRemaining - outByteSize)) & 31;
                    bitsRemaining -= outByteSize;
                    output += alphabet[index];
                }
            }
            if (bitsRemaining > 0)
            {
                int index = (buffer << (outByteSize - bitsRemaining)) & 31;
                output += alphabet[index];
            }
            return output;
        }

       public async Task SetOtpValidationTokenAsync(User user, string otpValidationToken)
        {
            user.OtpValidationToken = otpValidationToken;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }



        private static string CreatePasswordHash(string password)
        {
            // Генерация случайной соли
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);


            // Создание хеша
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8);

            // Объединяем соль и хеш для хранения
            byte[] hashBytes = new byte[salt.Length + hash.Length];
            Array.Copy(salt, 0, hashBytes, 0, salt.Length);
            Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);

            return Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPasswordHash(string password, string storedHash)
        {
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            // Извлекаем соль (первые 16 байт)
            byte[] salt = new byte[128 / 8];
            Array.Copy(hashBytes, 0, salt, 0, salt.Length);

            // Создаем хеш из введенного пароля
            byte[] computedHash = KeyDerivation.Pbkdf2(
                password: password!,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8);

            // Извлекаем оригинальный хеш для сравнения
            byte[] originalHash = new byte[hashBytes.Length - salt.Length];
            Array.Copy(hashBytes, salt.Length, originalHash, 0, originalHash.Length);

            // Сравниваем хеши
            return originalHash.SequenceEqual(computedHash);
        }
    }
}
