namespace WebAPI.Domain.Entities;

public class Dzial
{
    public long Id { get; set; }
    public string Nazwa { get; set; } = null!;

    public long FirmaId { get; set; }
    public Firma Firma { get; set; } = null!;

    public ICollection<Pracownik> Pracownicy { get; set; } = new List<Pracownik>();
}
