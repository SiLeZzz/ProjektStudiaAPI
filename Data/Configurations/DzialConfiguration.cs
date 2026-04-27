using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Entities;

namespace WebAPI.Data.Configurations;

public class DzialConfiguration : IEntityTypeConfiguration<Dzial>
{
    public void Configure(EntityTypeBuilder<Dzial> builder)
    {
        builder.ToTable("DZIALY");

        builder.Property(d => d.Id)
            .HasColumnName("id_dzialu");

        builder.Property(d => d.Nazwa)
            .HasColumnName("nazwa")
            .HasMaxLength(200);

        builder.Property(d => d.FirmaId)
            .HasColumnName("id_firmy");

        builder.HasOne(d => d.Firma)
            .WithMany(f => f.Dzialy)
            .HasForeignKey(d => d.FirmaId)
            .HasConstraintName("FK_DZIALY_FIRMY_id_firmy");
    }
}
