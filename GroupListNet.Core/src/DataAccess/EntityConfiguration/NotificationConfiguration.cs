using GroupListNet.Core.src.Entities;
using GroupListNet.Core.src.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class NotificationConfiguration : BaseEntityConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            base.Configure(builder);
            builder.ToTable("notifications");
            builder.Property(p => p.NotificationType).HasColumnName("notification_type").IsRequired();
            builder.Property(p => p.Messenger).HasColumnName("messenger").IsRequired().HasDefaultValue(MessengerType.Telegram);
            builder.Property(p => p.IsSent).HasColumnName("is_sent").IsRequired().HasDefaultValue(false);
            builder.Property(p => p.SentAt).HasColumnName("sent_at");
            builder.Property(p => p.ErrorMessage).HasColumnName("error_message");

            builder.HasOne(p => p.Student)
                   .WithMany()
                   .HasForeignKey(p => p.StudentId)
                   .IsRequired();

            builder.HasOne(p => p.Schedule)
                   .WithMany()
                   .HasForeignKey(p => p.ScheduleId);
        }
    }
}
