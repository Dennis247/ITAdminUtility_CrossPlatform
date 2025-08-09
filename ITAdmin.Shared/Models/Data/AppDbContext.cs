using Microsoft.EntityFrameworkCore;
using ITAdmin.Shared.Models;
using System;
using System.IO;

namespace ITAdmin.Shared.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserLogin> UserLogins { get; set; }
        public DbSet<SystemCheckResult> SystemCheckResults { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbPath;

                if (OperatingSystem.IsMacOS())
                {
                    // Use home directory for macOS
                    var home = Environment.GetEnvironmentVariable("HOME") ??
                               Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    dbPath = Path.Combine(home, ".itadminutility", "app.db");
                }
                else if (OperatingSystem.IsWindows())
                {
                    // Use C drive for Windows
                    dbPath = Path.Combine(@"C:\ITAdminUtility", "app.db");
                }
                else
                {
                    // Fallback for other platforms
                    var dataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    dbPath = Path.Combine(dataDir, "ITAdminUtility", "app.db");
                }

                // Ensure directory exists
                var directory = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserLogin entity
            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Configure SystemCheckResult entity
            modelBuilder.Entity<SystemCheckResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CheckTime).HasDefaultValueSql("datetime('now')");
            });
        }
    }
}