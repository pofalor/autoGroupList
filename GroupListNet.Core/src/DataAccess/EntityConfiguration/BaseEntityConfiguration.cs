using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GroupListNet.Core.src.Entities;

namespace GroupListNet.Core.src.DataAccess.EntityConfiguration
{
    public class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : PersistentEntity
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey("Id");
            builder.Property("Id").HasColumnName("id");
            builder.Property(x => x.ObjectCreateDate).HasColumnName("object_create_date").IsRequired();
            builder.Property(x => x.ObjectEditDate).HasColumnName("object_edit_date").IsRequired();
            builder.Property(x => x.Version).HasColumnName("version").IsRowVersion().HasDefaultValueSql("gen_random_bytes(8)");
            builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        }
    }
}
