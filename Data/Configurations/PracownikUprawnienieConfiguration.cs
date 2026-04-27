using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Entities;

namespace WebAPI.Data.Configurations;

public class PracownikUprawnienieConfiguration : IEntityTypeConfiguration<PracownikUprawnienie>
{
    public void Configure(EntityTypeBuilder<PracownikUprawnienie> builder)
    {
        builder.ToTable("POSIADA");

        builder.HasKey(pu => new { pu.PracownikId, pu.SygnaturaUprawnienia });

        builder.Property(pu => pu.PracownikId)
            .HasColumnName("id_pracownika");

        builder.Property(pu => pu.SygnaturaUprawnienia)
            .HasColumnName("sygnatura")
            .HasMaxLength(100);

        builder.Property(pu => pu.DataNadania)
            .HasColumnName("data_nadania");

        builder.Property(pu => pu.WazneDo)
            .HasColumnName("ważne_do");

        builder.HasOne(pu => pu.Pracownik)
            .WithMany(p => p.PracownikUprawnienia)
            .HasForeignKey(pu => pu.PracownikId)
            .HasConstraintName("FK_POSIADA_PRACOWNICY_id_pracownika");

        builder.HasOne(pu => pu.Uprawnienie)
            .WithMany(u => u.PracownikUprawnienia)
            .HasForeignKey(pu => pu.SygnaturaUprawnienia)
            .HasConstraintName("FK_POSIADA_UPRAWNIENIA_sygnatura");
    }
}
