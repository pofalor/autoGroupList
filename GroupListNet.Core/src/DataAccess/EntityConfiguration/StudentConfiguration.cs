using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class StudentConfiguration : BaseEntityConfiguration<Student>
    {
        public override void Configure(EntityTypeBuilder<Student> builder)
        {
            base.Configure(builder);
            builder.ToTable("students");
            builder.Property(p => p.Name).HasColumnName("name").IsRequired();
            builder.Property(p => p.SurName).HasColumnName("surname").IsRequired();
            builder.Property(p => p.FatherName).HasColumnName("father_name").IsRequired();
            builder.Property(p => p.NumberInGroup).HasColumnName("number_in_group").IsRequired();
            builder.Property(p => p.TelegramId).HasColumnName("telegram_id");
            builder.Property(p => p.VkId).HasColumnName("vk_id");
            builder.Property(p => p.Subgroup).HasColumnName("subgroup");
        }
    }
}
