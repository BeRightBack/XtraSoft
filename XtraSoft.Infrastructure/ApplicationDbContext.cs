using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using XtraSoft.Core;

namespace XtraSoft.Infrastructure
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<UserProfile, UserRole, string>(options)
    {

        // DbSet        
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<StringResource> StringResources { get; set; }
        public virtual DbSet<VisitorLog> VisitorLogs { get; set; }
        public virtual DbSet<VerificationCodeRecord> VerificationCodes { get; set; }
        public virtual DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserProfile>(entity =>
            {
                entity.ToTable(name: "User");
            });
            builder.Entity<UserRole>(entity =>
            {
                entity.ToTable(name: "Role");
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });
            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });

            builder.Entity<Language>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(50);
            });

            builder.Entity<StringResource>(entity =>
            {
                entity.Property(e => e.Name)
                      .HasMaxLength(500)
                      .UseCollation("utf8mb4_general_ci"); // case-insensitive

                entity.Property(e => e.Value).HasMaxLength(500);

                entity.HasOne(d => d.Language)
                    .WithMany(p => p.StringResources)
                    .HasForeignKey(d => d.LanguageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.LanguageId, e.Name });
            });


            builder.Entity<VisitorLog>(entity =>
            {
                entity.Property(e => e.Country).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.City).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.Region).HasMaxLength(100).IsRequired(false);
            });

            
            builder.Entity<VerificationCodeRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt });

                // Set default values - Fixed for MySQL
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
                entity.Property(e => e.ExpiresAt).IsRequired();
            });

            builder.Entity<Book>().ToTable("Book");

        }
    }

    // Minimal POCOs/interfaces for compilation

    public record SmtpSettings(string From, string Host, int Port, string Username, string Password);
    public record IpGeolocationOptions(string ApiKey);
    public record DeepLOptions(string AuthKey, string SourceLang, string TargetLangEn);
}
