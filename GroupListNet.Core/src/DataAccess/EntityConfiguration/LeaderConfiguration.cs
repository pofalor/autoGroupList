using GroupListNet.Core.src.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class LeaderConfiguration : BaseEntityConfiguration<Leader>
    {
        public override void Configure(EntityTypeBuilder<Leader> builder)
        {
            base.Configure(builder);
            builder.ToTable("leader");

            builder.HasOne(p => p.Student)
                   .WithMany()
                   .HasForeignKey(p => p.StudentId)
                   .IsRequired(true);
        }
    }
}
