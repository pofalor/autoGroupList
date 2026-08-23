using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class SubjectConfiguration : BaseEntityConfiguration<Subject>
    {
        public override void Configure(EntityTypeBuilder<Subject> builder)
        {
            base.Configure(builder);
            builder.ToTable("subjects");
            builder.Property(p => p.Name).HasColumnName("name").IsRequired();
        }
    }
}