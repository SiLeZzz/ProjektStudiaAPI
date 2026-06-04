namespace WebAPI.Contracts.Departments;

public class CreateDepartmentRequest
{
    public string Nazwa { get; set; } = null!;
    public long FirmaId { get; set; }
}
