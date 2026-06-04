namespace WebAPI.Contracts.Companies;

public class CompanyResponse
{
    public long Id { get; set; }
    public string Nazwa { get; set; } = null!;
    public string Nip { get; set; } = null!;
}
