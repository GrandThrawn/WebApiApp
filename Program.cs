using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using WebApiApp.Data;
using WebApiApp.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebApiApp.Middleware;
using WebApiApp.Interfaces;
using WebApiApp.Settings;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login"; // Путь для перенаправления на логин
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Время жизни куки
        options.SlidingExpiration = true; // Автоматическое продление куки при активности
        options.Cookie.Name = "SlonenaWebApi";
        options.SlidingExpiration = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; //HTTPS
    });

//coonection to db
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//email
var smtpConfig = builder.Configuration.GetSection("Smtp").Get<SmtpConfig>();
builder.Services.AddSingleton<IEmailSender>(new EmailSender(
    smtpConfig.Server,
    int.Parse(smtpConfig.Port),
    smtpConfig.User,
    smtpConfig.Pass
));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
