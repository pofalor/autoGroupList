using Microsoft.EntityFrameworkCore;
using GroupListNet.Core.src.DataAccess.EntityConfiguration;
using GroupListNet.Utils.src.Extensions;
using GroupListNet.Core.src.Entities;

namespace GroupListNet.Core.src.DataAccess
{
    /// <summary>
    /// DbContext приложения
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LeaderConfiguration());
            modelBuilder.ApplyConfiguration(new AttendanceConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new ScheduleConfiguration());
            modelBuilder.ApplyConfiguration(new StudentConfiguration());
            modelBuilder.ApplyConfiguration(new SubjectConfiguration());
            modelBuilder.ApplyConfiguration(new GroupSettingsConfiguration());
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            UpdatePersistentEntitiesData();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            UpdatePersistentEntitiesData();

            return base.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override int SaveChanges()
        {
            UpdatePersistentEntitiesData();

            return base.SaveChanges();
        }

        private void UpdatePersistentEntitiesData()
        {
            var addedEntries = ChangeTracker
                .Entries<PersistentEntity>()
                .Where(x => x.State == EntityState.Added);

            var now = DateTime.UtcNow;

            addedEntries.Foreach(entry =>
            {
                entry.Entity.ObjectCreateDate = now;
                entry.Entity.ObjectEditDate = now;
            });

            var updateEntries = ChangeTracker
                .Entries<PersistentEntity>()
                .Where(x => x.State == EntityState.Modified);

            updateEntries.Foreach(entry =>
            {
                entry.Entity.ObjectEditDate = now;
            });
        }
    }
}