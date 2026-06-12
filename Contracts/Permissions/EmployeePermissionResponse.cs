namespace WebAPI.Contracts.Permissions;

public class EmployeePermissionResponse
{
    public string Sygnatura { get; set; } = null!;
    public string Nazwa { get; set; } = null!;
    public string Opis { get; set; } = null!;
    public bool CzyPrzypisane { get; set; }
    public DateOnly? DataNadania { get; set; }
    public DateOnly? WazneDo { get; set; }
}
