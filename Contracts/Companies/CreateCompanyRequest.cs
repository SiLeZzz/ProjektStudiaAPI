namespace WebAPI.Contracts.Companies;

public class CreateCompanyRequest
{
    public string Nazwa { get; set; } = null!;
    public string Nip { get; set; } = null!;
}
