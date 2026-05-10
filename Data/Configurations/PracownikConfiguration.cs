using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Entities;

namespace WebAPI.Data.Configurations;

public class PracownikConfiguration : IEntityTypeConfiguration<Pracownik>
{
    public void Configure(EntityTypeBuilder<Pracownik> builder)
    {
        builder.ToTable("PRACOWNICY");

        builder.Property(p => p.Id)
            .HasColumnName("id_pracownika");

        builder.Property(p => p.Imie)
            .HasColumnName("imie")
            .HasMaxLength(100);

        builder.Property(p => p.Nazwisko)
            .HasColumnName("nazwisko")
            .HasMaxLength(100);

        builder.Property(p => p.Login)
            .HasColumnName("login")
            .HasMaxLength(100);

        builder.HasIndex(p => p.Login)
            .IsUnique();

        builder.Property(p => p.HasloHash)
            .HasColumnName("hash_hasla")
            .HasMaxLength(512);

        builder.Property(p => p.Rola)
            .HasColumnName("rola")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.CzyAktywny)
            .HasColumnName("czy_aktywny");

        builder.Property(p => p.DzialId)
            .HasColumnName("id_dzialu");

        builder.Property(p => p.StanowiskoId)
            .HasColumnName("id_stanowiska");

        builder.HasOne(p => p.Dzial)
            .WithMany(d => d.Pracownicy)
            .HasForeignKey(p => p.DzialId)
            .HasConstraintName("FK_PRACOWNICY_DZIALY_id_dzialu");

        builder.HasOne(p => p.Stanowisko)
            .WithMany(s => s.Pracownicy)
            .HasForeignKey(p => p.StanowiskoId)
            .HasConstraintName("FK_PRACOWNICY_STANOWISKA_id_stanowiska");
    }
}
