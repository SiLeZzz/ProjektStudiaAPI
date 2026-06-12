namespace WebAPI.Contracts.Employees;

public class EmployeeResponse
{
    public long Id { get; set; }
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Login { get; set; } = null!;
    public string Rola { get; set; } = null!;
    public bool CzyAktywny { get; set; }
    public long DzialId { get; set; }
    public string Dzial { get; set; } = null!;
    public long FirmaId { get; set; }
    public string Firma { get; set; } = null!;
    public long StanowiskoId { get; set; }
    public string Stanowisko { get; set; } = null!;
}
