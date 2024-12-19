using AccessControl.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace AccessControl.Infrastructure.Persistence.DbContext
{
    public class AccessControlDbContext: IdentityDbContext<UserEntity, IdentityRole<Guid>, Guid>
    {

        public AccessControlDbContext() { }

        public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
        : base(options) { }

        public virtual DbSet<LogEntity> LogEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<LogEntity>()
                .HasOne(log => log.UserEntity)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(builder);

            builder.Entity<UserEntity>().ToTable("Users");

            builder.Ignore<IdentityRole<Guid>>();
            builder.Ignore<IdentityUserClaim<Guid>>();
            builder.Ignore<IdentityUserLogin<Guid>>();
            builder.Ignore<IdentityUserRole<Guid>>();
            builder.Ignore<IdentityRoleClaim<Guid>>();
            builder.Ignore<IdentityUserToken<Guid>>();
        }
    }
}
