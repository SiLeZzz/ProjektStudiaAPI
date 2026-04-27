using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Entities;

namespace WebAPI.Data.Configurations;

public class FirmaConfiguration : IEntityTypeConfiguration<Firma>
{
    public void Configure(EntityTypeBuilder<Firma> builder)
    {
        builder.ToTable("FIRMY");

        builder.Property(f => f.Id)
            .HasColumnName("id_firmy");

        builder.Property(f => f.Nazwa)
            .HasColumnName("nazwa")
            .HasMaxLength(200);

        builder.Property(f => f.Nip)
            .HasColumnName("nip")
            .HasMaxLength(10);

        builder.HasIndex(f => f.Nip)
            .IsUnique();
    }
}
