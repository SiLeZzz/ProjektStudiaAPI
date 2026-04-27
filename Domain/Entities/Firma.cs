namespace WebAPI.Domain.Entities;

public class Firma
{
    public long Id { get; set; }
    public string Nazwa { get; set; } = null!;
    public string Nip { get; set; } = null!;

    public ICollection<Dzial> Dzialy { get; set; } = new List<Dzial>();
}
