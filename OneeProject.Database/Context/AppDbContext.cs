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

        #region DataTables
        public DbSet<Category> Categories { get; set; }
        public DbSet<WorkerCategory> WorkerCategories { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<JobChatMessage> JobChatMessages { get; set; }
        public DbSet<JobRating> JobRatings { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<SavedAddress> SavedAddresses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        #endregion

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

            // Composite Primary Key
            builder.Entity<WorkerCategory>()
                .HasKey(x => new { x.FK_user_ID, x.Category_id });

            // One rating per job
            builder.Entity<JobRating>()
                .HasIndex(r => r.FK_job_ID)
                .IsUnique();

            builder.Entity<DeviceToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            builder.Entity<DeviceToken>()
                .HasIndex(t => t.FK_user_ID);

            builder.Entity<SavedAddress>()
                .HasIndex(a => a.FK_user_ID);

            builder.Entity<Notification>()
                .HasIndex(n => new { n.FK_user_ID, n.Is_Read });

            builder.Entity<Notification>()
                .HasIndex(n => n.FK_job_ID);

            builder.Entity<Complaint>()
                .HasIndex(c => c.FK_job_ID)
                .IsUnique();
        }
    }
}
