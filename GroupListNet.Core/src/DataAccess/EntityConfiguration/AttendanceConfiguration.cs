using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class AttendanceConfiguration : BaseEntityConfiguration<Attendance>
    {
        public override void Configure(EntityTypeBuilder<Attendance> builder)
        {
            base.Configure(builder);
            builder.ToTable("attendance");
            builder.Property(p => p.Date).HasColumnName("date").IsRequired();

            builder.HasOne(p => p.Student)
                   .WithMany()
                   .HasForeignKey(p => p.StudentId)
                   .IsRequired();

            builder.HasOne(p => p.Schedule)
                   .WithMany()
                   .HasForeignKey(p => p.ScheduleId)
                   .IsRequired();
        }
    }
}
