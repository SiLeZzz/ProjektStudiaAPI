namespace WebAPI.Domain.Entities;

public class Uprawnienie
{
    public string Sygnatura { get; set; } = null!;
    public string Nazwa { get; set; } = null!;
    public string Opis { get; set; } = null!;

    public long PracownikZarzadzajacyId { get; set; }
    public Pracownik PracownikZarzadzajacy { get; set; } = null!;

    public ICollection<PracownikUprawnienie> PracownikUprawnienia { get; set; } = new List<PracownikUprawnienie>();
}
