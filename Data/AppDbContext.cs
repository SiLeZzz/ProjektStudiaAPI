using Microsoft.EntityFrameworkCore;
using WebAPI.Domain.Entities;

namespace WebAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Firma> Firmy => Set<Firma>();
    public DbSet<Dzial> Dzialy => Set<Dzial>();
    public DbSet<Stanowisko> Stanowiska => Set<Stanowisko>();
    public DbSet<Pracownik> Pracownicy => Set<Pracownik>();
    public DbSet<Uprawnienie> Uprawnienia => Set<Uprawnienie>();
    public DbSet<PracownikUprawnienie> PracownikUprawnienia => Set<PracownikUprawnienie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
