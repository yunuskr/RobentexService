using System;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RobentexService.Models;

namespace RobentexService.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestNote>  ServiceRequestNotes  => Set<ServiceRequestNote>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog>      AuditLogs       => Set<AuditLog>();
    
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ServiceRequest tarih tipi
        b.Entity<ServiceRequest>()
            .Property(p => p.CreatedAt)
            .HasConversion<DateTime>(); // datetime2

        // AppUser: Username unique
        b.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // --- İSTEĞE BAĞLI: İlk admin seed (admin / Admin!123) ---
        // NOT: Seed'te sabit tarih kullan!
        var seedCreated = new DateTime(2025, 09, 19, 0, 0, 0, DateTimeKind.Utc);
        var adminHash   = PasswordHasher.Hash("Admin!123");

        b.Entity<AppUser>().HasData(new AppUser
        {
            Id = 1,
            Username = "admin",
            PasswordHash = adminHash,
            DisplayName = "Yönetici",
            IsActive = true,
            CreatedAtUtc = seedCreated,
            LastLoginUtc = null
        });
        // ----------------------------------------------------------
    }
}

/// <summary>
/// Hızlı başlangıç için SHA256 + sabit salt.
/// Üretimde PBKDF2/Bcrypt/Argon2 tercih edin.
/// </summary>
public static class PasswordHasher
{
    private const string Salt = "robentex-static-salt-v1";

    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(Salt + password);
        var hash  = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string password, string hashHex) =>
        Hash(password) == hashHex;
}
