namespace WebAPI.Domain.Entities;

public class Stanowisko
{
    public long Id { get; set; }
    public string Nazwa { get; set; } = null!;

    public ICollection<Pracownik> Pracownicy { get; set; } = new List<Pracownik>();
}
