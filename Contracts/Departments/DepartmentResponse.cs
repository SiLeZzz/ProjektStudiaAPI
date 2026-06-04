namespace WebAPI.Contracts.Departments;

public class DepartmentResponse
{
    public long Id { get; set; }
    public string Nazwa { get; set; } = null!;
    public long FirmaId { get; set; }
    public string FirmaNazwa { get; set; } = null!;
}
