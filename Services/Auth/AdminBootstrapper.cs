using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI.Configuration;
using WebAPI.Data;
using WebAPI.Domain.Entities;

namespace WebAPI.Services.Auth;

public class AdminBootstrapper(
    AppDbContext dbContext,
    IPasswordHasher<Pracownik> passwordHasher,
    IOptions<BootstrapAdminOptions> bootstrapOptions,
    ILogger<AdminBootstrapper> logger)
{
    private readonly BootstrapAdminOptions _bootstrapOptions = bootstrapOptions.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_bootstrapOptions.Password))
        {
            throw new InvalidOperationException("Bootstrap admin password is not configured.");
        }

        var existingAdmin = await dbContext.Pracownicy
            .FirstOrDefaultAsync(p => p.Login == _bootstrapOptions.Login, cancellationToken);

        if (existingAdmin is not null)
        {
            if (existingAdmin.Rola != EmployeeRole.Administrator || !existingAdmin.CzyAktywny)
            {
                existingAdmin.Rola = EmployeeRole.Administrator;
                existingAdmin.CzyAktywny = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var firma = await dbContext.Firmy
            .FirstOrDefaultAsync(f => f.Nip == _bootstrapOptions.FirmaNip, cancellationToken);

        if (firma is null)
        {
            firma = new Firma
            {
                Nazwa = _bootstrapOptions.FirmaNazwa,
                Nip = _bootstrapOptions.FirmaNip
            };

            dbContext.Firmy.Add(firma);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var dzial = await dbContext.Dzialy
            .FirstOrDefaultAsync(d => d.FirmaId == firma.Id && d.Nazwa == _bootstrapOptions.DzialNazwa, cancellationToken);

        if (dzial is null)
        {
            dzial = new Dzial
            {
                Nazwa = _bootstrapOptions.DzialNazwa,
                FirmaId = firma.Id
            };

            dbContext.Dzialy.Add(dzial);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var stanowisko = await dbContext.Stanowiska
            .FirstOrDefaultAsync(s => s.Nazwa == _bootstrapOptions.StanowiskoNazwa, cancellationToken);

        if (stanowisko is null)
        {
            stanowisko = new Stanowisko
            {
                Nazwa = _bootstrapOptions.StanowiskoNazwa
            };

            dbContext.Stanowiska.Add(stanowisko);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var admin = new Pracownik
        {
            Imie = _bootstrapOptions.Imie,
            Nazwisko = _bootstrapOptions.Nazwisko,
            Login = _bootstrapOptions.Login,
            Rola = EmployeeRole.Administrator,
            CzyAktywny = true,
            DzialId = dzial.Id,
            StanowiskoId = stanowisko.Id
        };

        admin.HasloHash = passwordHasher.HashPassword(admin, _bootstrapOptions.Password);

        dbContext.Pracownicy.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bootstrap administrator '{Login}' has been created.", admin.Login);
    }
}
