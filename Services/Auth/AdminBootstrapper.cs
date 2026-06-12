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
    private const string DefaultPermissionManagerLogin = "permission.manager";

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

            var manager = await EnsureDefaultPermissionManagerAsync(
                existingAdmin.DzialId,
                existingAdmin.StanowiskoId,
                cancellationToken);
            await EnsureDefaultPermissionsAsync(manager.Id, cancellationToken);
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

        var permissionManager = await EnsureDefaultPermissionManagerAsync(
            dzial.Id,
            stanowisko.Id,
            cancellationToken);
        await EnsureDefaultPermissionsAsync(permissionManager.Id, cancellationToken);

        logger.LogInformation("Bootstrap administrator '{Login}' has been created.", admin.Login);
    }

    private async Task<Pracownik> EnsureDefaultPermissionManagerAsync(
        long dzialId,
        long stanowiskoId,
        CancellationToken cancellationToken)
    {
        var manager = await dbContext.Pracownicy
            .FirstOrDefaultAsync(p => p.Login == DefaultPermissionManagerLogin, cancellationToken);

        if (manager is not null)
        {
            manager.Imie = "Pracownik";
            manager.Nazwisko = "Zarzadzajacy";
            manager.Rola = EmployeeRole.Pracownik;
            manager.CzyAktywny = true;
            manager.DzialId = dzialId;
            manager.StanowiskoId = stanowiskoId;
            await dbContext.SaveChangesAsync(cancellationToken);

            return manager;
        }

        manager = new Pracownik
        {
            Imie = "Pracownik",
            Nazwisko = "Zarzadzajacy",
            Login = DefaultPermissionManagerLogin,
            Rola = EmployeeRole.Pracownik,
            CzyAktywny = true,
            DzialId = dzialId,
            StanowiskoId = stanowiskoId
        };

        manager.HasloHash = passwordHasher.HashPassword(manager, _bootstrapOptions.Password);

        dbContext.Pracownicy.Add(manager);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bootstrap permission manager '{Login}' has been created.", manager.Login);

        return manager;
    }

    private async Task EnsureDefaultPermissionsAsync(long managerId, CancellationToken cancellationToken)
    {
        var defaults = new[]
        {
            new { Sygnatura = "MAG-001", Nazwa = "Dostep do magazynu", Opis = "Uprawnienie testowe do obszaru magazynu." },
            new { Sygnatura = "FAK-002", Nazwa = "Obsluga faktur", Opis = "Uprawnienie testowe do obslugi faktur." },
            new { Sygnatura = "ADM-003", Nazwa = "Panel administracyjny", Opis = "Uprawnienie testowe do funkcji administracyjnych." }
        };

        var defaultSignatures = defaults.Select(p => p.Sygnatura).ToList();
        var existingSignatures = await dbContext.Uprawnienia
            .Where(u => defaultSignatures.Contains(u.Sygnatura))
            .Select(u => u.Sygnatura)
            .ToListAsync(cancellationToken);

        var existingDefaultPermissions = await dbContext.Uprawnienia
            .Where(u => existingSignatures.Contains(u.Sygnatura))
            .ToListAsync(cancellationToken);

        foreach (var existingPermission in existingDefaultPermissions)
        {
            existingPermission.PracownikZarzadzajacyId = managerId;
        }

        foreach (var permission in defaults.Where(p => !existingSignatures.Contains(p.Sygnatura)))
        {
            dbContext.Uprawnienia.Add(new Uprawnienie
            {
                Sygnatura = permission.Sygnatura,
                Nazwa = permission.Nazwa,
                Opis = permission.Opis,
                PracownikZarzadzajacyId = managerId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
