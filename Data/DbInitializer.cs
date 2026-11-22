using System.Security.Claims;
using CyberProjekt.Data;
using CyberProjekt.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CyberProjekt.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            AppDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            await context.Database.MigrateAsync();

            foreach (var roleName in new[] { "Administrator", "Pracownik", "Gość" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            if (!await context.SecurityPolicies.AnyAsync())
            {
                context.SecurityPolicies.Add(new SecurityPolicy
                {
                    MinPasswordLength = 12,
                    IsEntropyPolicyEnabled = true,
                    MinEntropyBits = 60.0,
                    PasswordMaxAgeDays = 90,
                    SessionIdleTimeoutMinutes = 15
                });
                await context.SaveChangesAsync();
            }

            const string adminLogin = "ADMIN";
            const string adminPass = "Admin#SecurePass2026!";
            var adminUser = await userManager.FindByNameAsync(adminLogin);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminLogin,
                    Email = "admin@local.test",
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Admin",
                    MustChangePassword = false,
                    PasswordChangedAt = DateTime.UtcNow
                };

                if ((await userManager.CreateAsync(adminUser, adminPass)).Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                    await EnsureNameClaimsAsync(userManager, adminUser);
                    context.PasswordHistories.Add(new PasswordHistory
                    {
                        UserId = adminUser.Id,
                        PasswordHash = adminUser.PasswordHash ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                await EnsureNameClaimsAsync(userManager, adminUser);
            }

            // 9 użytkowników testowych dla Wariantu 8 (Administrator, Pracownik, Gość)
            var testUsers = new[]
            {
                ("admin.test1", "admin1@local.test", "Piotr", "Zieliński", "Administrator", true, false),
                ("admin.test2", "admin2@local.test", "Ewa", "Kaczmarek", "Administrator", false, false),
                ("pracownik1", "pracownik1@local.test", "Jan", "Kowalski", "Pracownik", true, false),
                ("pracownik2", "pracownik2@local.test", "Anna", "Nowak", "Pracownik", false, false),
                ("pracownik3", "pracownik3@local.test", "Marek", "Wójcik", "Pracownik", false, false),
                ("pracownik4", "pracownik4@local.test", "Katarzyna", "Kamińska", "Pracownik", false, true),
                ("gosc1", "gosc1@local.test", "Tomasz", "Lewandowski", "Gość", true, false),
                ("gosc2", "gosc2@local.test", "Magdalena", "Dąbrowska", "Gość", false, false),
                ("gosc3", "gosc3@local.test", "Łukasz", "Szymański", "Gość", false, false)
            };

            const string testPass = "12345678";
            foreach (var (login, email, first, last, role, mustChange, blocked) in testUsers)
            {
                var existingUser = await userManager.FindByNameAsync(login);
                if (existingUser != null)
                {
                    await EnsureNameClaimsAsync(userManager, existingUser);
                    continue;
                }

                var user = new User
                {
                    UserName = login,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = first,
                    LastName = last,
                    MustChangePassword = mustChange,
                    IsBlocked = blocked,
                    PasswordChangedAt = DateTime.UtcNow
                };

                if ((await userManager.CreateAsync(user, testPass)).Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    await EnsureNameClaimsAsync(userManager, user);
                    context.PasswordHistories.Add(new PasswordHistory
                    {
                        UserId = user.Id,
                        PasswordHash = user.PasswordHash ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();

            if (await context.Projects.AnyAsync()) return;

            var allUsers = await context.Users.Where(u => u.Email != "unknown@system.local").ToListAsync();
            if (!allUsers.Any()) return;

            var guestUsers = await userManager.GetUsersInRoleAsync("Gość");
            var guestIds = guestUsers.Select(g => g.Id).ToHashSet();
            var assignableUsers = allUsers.Where(u => !guestIds.Contains(u.Id)).ToList();
            if (!assignableUsers.Any()) assignableUsers = allUsers;

            var owner = adminUser ?? assignableUsers.First();
            var random = new Random();
            var projectNames = new[]
            {
                "System CRM", "Aplikacja Mobilna", "Panel Administratora", "Strona Wizytówka",
                "Sklep Internetowy", "Integracja API", "Migracja Chmurowa", "Audyt Bezpieczeństwa"
            };

            foreach (var name in projectNames)
            {
                var project = new Project
                {
                    Name = name,
                    Description = $"Opis projektu {name}",
                    StartDate = DateTime.Now.AddDays(-30),
                    EndDate = DateTime.Now.AddDays(60),
                    UserId = owner.Id
                };
                context.Projects.Add(project);
                await context.SaveChangesAsync();

                for (int j = 0; j < 4; j++)
                {
                    var task = new ProjectTask
                    {
                        Title = $"Zadanie {j + 1} - {name}",
                        Description = "Szczegółowy opis zadania testowego.",
                        Priority = (TaskPriority)random.Next(0, 4),
                        Status = (Models.TaskStatus)random.Next(0, 4),
                        Deadline = DateTime.Now.AddDays(random.Next(1, 30)),
                        ProjectId = project.ProjectId,
                        CreatorId = owner.Id,
                        AssignedUserId = assignableUsers[random.Next(0, assignableUsers.Count)].Id
                    };
                    context.Tasks.Add(task);
                    await context.SaveChangesAsync();

                    for (int k = 0; k < 2; k++)
                    {
                        context.Comments.Add(new Comment
                        {
                            Content = "Przykładowy komentarz do zadania.",
                            TaskId = task.TaskId,
                            UserId = allUsers[random.Next(0, allUsers.Count)].Id
                        });
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureNameClaimsAsync(UserManager<User> userManager, User user)
        {
            var claims = await userManager.GetClaimsAsync(user);
            if (!claims.Any(c => c.Type == "FirstName"))
            {
                await userManager.AddClaimAsync(user, new Claim("FirstName", user.FirstName));
            }
            if (!claims.Any(c => c.Type == "LastName"))
            {
                await userManager.AddClaimAsync(user, new Claim("LastName", user.LastName));
            }
        }
    }
}