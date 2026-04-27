using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Entities;

namespace WebAPI.Data.Configurations;

public class UprawnienieConfiguration : IEntityTypeConfiguration<Uprawnienie>
{
    public void Configure(EntityTypeBuilder<Uprawnienie> builder)
    {
        builder.ToTable("UPRAWNIENIA");

        builder.HasKey(u => u.Sygnatura);

        builder.Property(u => u.Sygnatura)
            .HasColumnName("sygnatura")
            .HasMaxLength(100);

        builder.Property(u => u.Nazwa)
            .HasColumnName("nazwa")
            .HasMaxLength(200);

        builder.Property(u => u.Opis)
            .HasColumnName("opis")
            .HasMaxLength(1000);

        builder.Property(u => u.PracownikZarzadzajacyId)
            .HasColumnName("id_pracownika_zarzadzajacego");

        builder.HasOne(u => u.PracownikZarzadzajacy)
            .WithMany(p => p.ZarzadzaneUprawnienia)
            .HasForeignKey(u => u.PracownikZarzadzajacyId)
            .HasConstraintName("FK_UPRAWNIENIA_PRACOWNICY_id_pracownika_zarzadzajacego");
    }
}
