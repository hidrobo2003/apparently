using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using AppaRently.Infrastructure.Services;
using AppaRently.Infrastructure.Services.Security;
using AppaRently.Infrastructure.Services.Notifications;
using AppaRently.Infrastructure.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Text;
using System.Security.Claims;

namespace AppaRently.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppaRentlyInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AppaRentlyDb")
            ?? throw new InvalidOperationException("Connection string 'AppaRentlyDb' was not configured.");

        services.AddDbContext<AppaRentlyDbContext>(options =>
            options.UseNpgsql(connectionString));

        var dataProtectionKeyPath = configuration["DataProtection:KeyPath"];
        if (string.IsNullOrWhiteSpace(dataProtectionKeyPath))
        {
            dataProtectionKeyPath = Path.Combine(
                Path.GetTempPath(),
                "AppaRently",
                "DataProtection-Keys");
        }

        Directory.CreateDirectory(dataProtectionKeyPath);
        services.AddDataProtection()
            .SetApplicationName("AppaRently")
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

        services
            .AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppaRentlyDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                    ?? throw new InvalidOperationException("JWT configuration was not provided.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue(jwtOptions.CookieName, out var token) &&
                            !string.IsNullOrWhiteSpace(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IApartmentService, ApartmentService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<IAppNotificationService, AppNotificationService>();
        services.AddScoped<AppaRentlySeeder>();

        var smtpOptions = configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>();
        if (smtpOptions?.EnableReminderWorker == true)
        {
            services.AddHostedService<ReservationReminderBackgroundService>();
        }

        return services;
    }
}
