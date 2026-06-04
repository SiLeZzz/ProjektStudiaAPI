namespace WebAPI.Contracts.Companies;

public class UpdateCompanyRequest
{
    public string Nazwa { get; set; } = null!;
    public string Nip { get; set; } = null!;
}
