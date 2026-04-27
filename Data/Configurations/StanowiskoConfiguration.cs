using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAPI.Domain.Entities;

namespace WebAPI.Data.Configurations;

public class StanowiskoConfiguration : IEntityTypeConfiguration<Stanowisko>
{
    public void Configure(EntityTypeBuilder<Stanowisko> builder)
    {
        builder.ToTable("STANOWISKA");

        builder.Property(s => s.Id)
            .HasColumnName("id_stanowiska");

        builder.Property(s => s.Nazwa)
            .HasColumnName("nazwa")
            .HasMaxLength(200);
    }
}
