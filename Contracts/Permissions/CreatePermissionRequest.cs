namespace WebAPI.Contracts.Permissions;

public class CreatePermissionRequest
{
    public string Sygnatura { get; set; } = null!;
    public string Nazwa { get; set; } = null!;
    public string Opis { get; set; } = null!;
    public long PracownikZarzadzajacyId { get; set; }
}
