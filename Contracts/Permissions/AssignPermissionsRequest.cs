namespace WebAPI.Contracts.Permissions;

public class AssignPermissionsRequest
{
    public IReadOnlyCollection<string> SygnaturyUprawnien { get; set; } = [];
    public DateOnly? WazneDo { get; set; }
}
