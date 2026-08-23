using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class GroupSettingsConfiguration : BaseEntityConfiguration<GroupSettings>
    {
        public override void Configure(EntityTypeBuilder<GroupSettings> builder)
        {
            base.Configure(builder);
            builder.ToTable("group_settings");
            // ValueGeneratedNever нужен, чтобы EF не подставлял вместо false значение по умолчанию из базы
            builder.Property(p => p.HasSubgroups)
                .HasColumnName("has_subgroups")
                .IsRequired()
                .HasDefaultValue(true)
                .ValueGeneratedNever();
        }
    }
}
