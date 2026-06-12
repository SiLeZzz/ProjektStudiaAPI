using WebAPI.Domain.Entities;

namespace WebAPI.Contracts.Employees;

public class UpdateEmployeeRequest
{
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Login { get; set; } = null!;
    public string? Password { get; set; }
    public EmployeeRole Rola { get; set; }
    public bool CzyAktywny { get; set; }
    public long DzialId { get; set; }
    public long StanowiskoId { get; set; }
}
