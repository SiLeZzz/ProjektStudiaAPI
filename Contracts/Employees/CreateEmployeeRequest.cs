using WebAPI.Domain.Entities;

namespace WebAPI.Contracts.Employees;

public class CreateEmployeeRequest
{
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Login { get; set; } = null!;
    public string Password { get; set; } = null!;
    public EmployeeRole Rola { get; set; } = EmployeeRole.Pracownik;
    public long DzialId { get; set; }
    public long StanowiskoId { get; set; }
}
