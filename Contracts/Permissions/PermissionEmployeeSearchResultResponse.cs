namespace WebAPI.Contracts.Permissions;

public class PermissionEmployeeSearchResultResponse
{
    public long Id { get; set; }
    public string Numer { get; set; } = null!;
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Dzial { get; set; } = null!;
    public string Firma { get; set; } = null!;
}
