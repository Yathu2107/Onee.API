using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OneeProject.Database.Context
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
       public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ✅ Global collation for MySQL (case-insensitive)
            builder.UseCollation("utf8mb4_general_ci");

            // ✅ Rename Identity tables (optional)
            builder.Entity<AppUser>().ToTable("t_user").Property(p => p.Id).HasColumnName("RnUserID");
            builder.Entity<IdentityRole>().ToTable("Tbl_Role");
            builder.Entity<IdentityUserRole<string>>().ToTable("Tbl_User_Role");
            builder.Entity<IdentityUserLogin<string>>().ToTable("Tbl_User_Login");
            builder.Entity<IdentityUserClaim<string>>().ToTable("Tbl_User_Claims");
            builder.Entity<IdentityUserToken<string>>().ToTable("Tbl_User_Token");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("Tbl_Role_Claims");

            // ✅ Seed roles
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "d3f1a7b2-8c4e-4f3a-9a1e-1a2b3c4d5e6f",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "f6e5d4c3-b2a1-0987-fedc-ba0987654321",
                    Name = "User",
                    NormalizedName = "USER"
                },
                new IdentityRole
                {
                    Id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                    Name = "Worker",
                    NormalizedName = "WORKER"
                }
            );

            // ✅ Unique constraint on Email + Mobile
            builder.Entity<AppUser>()
                .HasIndex(u => new { u.Email, u.PhoneNumber })
                .IsUnique();
        }
    }
}
