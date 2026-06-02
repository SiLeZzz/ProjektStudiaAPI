namespace WebAPI.Contracts.Permissions;

public class UpdatePermissionRequest
{
    public string Nazwa { get; set; } = null!;
    public string Opis { get; set; } = null!;
    public long PracownikZarzadzajacyId { get; set; }
}
