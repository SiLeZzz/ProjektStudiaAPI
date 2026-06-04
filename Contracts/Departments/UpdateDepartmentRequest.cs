namespace WebAPI.Contracts.Departments;

public class UpdateDepartmentRequest
{
    public string Nazwa { get; set; } = null!;
    public long FirmaId { get; set; }
}
