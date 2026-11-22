using CyberProjekt.Data;
using CyberProjekt.Models;
using CyberProjekt.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CyberProjekt
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddIdentity<User, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;

                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 1;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddScoped<IPasswordHasher<User>, Argon2PasswordHasher>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Administrator"));
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Home/Index";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;

                options.Events.OnSigningIn = async context =>
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                    var policy = await db.SecurityPolicies.FirstOrDefaultAsync();
                    var timeoutMinutes = policy?.SessionIdleTimeoutMinutes ?? 15;

                    context.Properties.Items["LastActivityUtc"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(timeoutMinutes);
                };

                options.Events.OnValidatePrincipal = async context =>
                {
                    if (context.Properties.Items.TryGetValue("LastActivityUtc", out var lastActivityStr) &&
                        long.TryParse(lastActivityStr, out var lastActivityUnix))
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var policy = await db.SecurityPolicies.FirstOrDefaultAsync();
                        var timeoutMinutes = policy?.SessionIdleTimeoutMinutes ?? 15;

                        var lastActivity = DateTimeOffset.FromUnixTimeSeconds(lastActivityUnix);
                        if (DateTimeOffset.UtcNow - lastActivity > TimeSpan.FromMinutes(timeoutMinutes))
                        {
                            context.RejectPrincipal();
                            context.HttpContext.Items["SessionExpired"] = true;
                            context.HttpContext.Session.Clear();
                            context.HttpContext.Response.Cookies.Delete(".AspNetCore.Session");
                            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                            return;
                        }

                        context.Properties.Items["LastActivityUtc"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(timeoutMinutes);
                        context.ShouldRenew = true;
                    }
                };

                options.Events.OnRedirectToLogin = context =>
                {
                    var redirectUri = context.RedirectUri;
                    if (context.HttpContext.Items.ContainsKey("SessionExpired") &&
                        !redirectUri.Contains("expired=1", StringComparison.OrdinalIgnoreCase))
                    {
                        redirectUri += redirectUri.Contains('?') ? "&expired=1" : "?expired=1";
                    }
                    context.Response.Redirect(redirectUri);
                    return Task.CompletedTask;
                };
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(15);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<PasswordSecurityService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<AppDbContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                await DbInitializer.Initialize(context, userManager, roleManager);
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }
    }
}