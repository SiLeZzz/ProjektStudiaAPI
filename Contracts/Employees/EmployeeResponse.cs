namespace WebAPI.Contracts.Employees;

public class EmployeeResponse
{
    public long Id { get; set; }
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Rola { get; set; } = null!;
    public string Dzial { get; set; }
    public string Stanowisko { get; set; }
}
