namespace WebAPI.Contracts.Employees;

public class EmployeeResponse
{
    public long Id { get; set; }
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Login { get; set; } = null!;
    public string Rola { get; set; } = null!;
    public long DzialId { get; set; }
    public long StanowiskoId { get; set; }
    public bool CzyAktywny { get; set; }
}
