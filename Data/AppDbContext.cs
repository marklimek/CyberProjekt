using CyberProjekt.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CyberProjekt.Models;

namespace CyberProjekt.Data;

    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> Tasks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<SecurityPolicy> SecurityPolicies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PasswordHistory>()
                .HasOne(ph => ph.User)
                .WithMany(u => u.PasswordHistories)
                .HasForeignKey(ph => ph.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTask>().HasQueryFilter(t => !t.IsDeleted);

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .IsRequired(false);

            modelBuilder.Entity<TaskHistory>()
                .HasOne(h => h.Task)
                .WithMany(t => t.Histories)
                .IsRequired(false);
        }
    }