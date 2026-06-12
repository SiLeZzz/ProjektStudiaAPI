namespace WebAPI.Contracts.Permissions;

public class PermissionResponse
{
    public string Sygnatura { get; set; } = null!;
    public string Nazwa { get; set; } = null!;
    public string Opis { get; set; } = null!;
    public long PracownikZarzadzajacyId { get; set; }
    public string PracownikZarzadzajacy { get; set; } = null!;
}
