using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class ScheduleConfiguration : BaseEntityConfiguration<Schedule>
    {
        public override void Configure(EntityTypeBuilder<Schedule> builder)
        {
            base.Configure(builder);
            builder.ToTable("schedule");
            builder.Property(p => p.Subgroup).HasColumnName("subgroup");
            builder.Property(p => p.WeekType).HasColumnName("week_type");
            builder.Property(p => p.DayOfWeek).HasColumnName("day_of_week");
            builder.Property(p => p.Date).HasColumnName("date");
            builder.Property(p => p.StartTime).HasColumnName("start_time").IsRequired();
            builder.Property(p => p.EndTime).HasColumnName("end_time").IsRequired();
            builder.Property(p => p.ClassType).HasColumnName("class_type").IsRequired();
            builder.Property(p => p.Room).HasColumnName("room");
            builder.Property(p => p.Building).HasColumnName("building");

            builder.HasOne(p => p.Subject)
                   .WithMany()
                   .HasForeignKey(p => p.SubjectId)
                   .IsRequired();
        }
    }
}