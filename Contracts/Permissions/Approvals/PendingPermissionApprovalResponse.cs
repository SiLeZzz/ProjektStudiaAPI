namespace WebAPI.Contracts.Permissions.Approvals;

public class PendingPermissionApprovalResponse
{
    public long EmployeeId { get; set; }
    public string EmployeeNumber { get; set; } = null!;
    public string EmployeeName { get; set; } = null!;
    public string Department { get; set; } = null!;
    public string Company { get; set; } = null!;
    public string PermissionSignature { get; set; } = null!;
    public string PermissionName { get; set; } = null!;
    public string PermissionDescription { get; set; } = null!;
    public DateOnly AssignedAt { get; set; }
    public DateOnly? ValidUntil { get; set; }
}
