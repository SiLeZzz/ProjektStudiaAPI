namespace WebAPI.Domain.Entities;

public class Pracownik
{
    public long Id { get; set; }
    public string Imie { get; set; } = null!;
    public string Nazwisko { get; set; } = null!;
    public string Login { get; set; } = null!;
    public string HasloHash { get; set; } = null!;
    public EmployeeRole Rola { get; set; }
    public bool CzyAktywny { get; set; } = true;

    public long DzialId { get; set; }
    public Dzial Dzial { get; set; } = null!;

    public long StanowiskoId { get; set; }
    public Stanowisko Stanowisko { get; set; } = null!;

    public ICollection<Uprawnienie> ZarzadzaneUprawnienia { get; set; } = new List<Uprawnienie>();
    public ICollection<PracownikUprawnienie> PracownikUprawnienia { get; set; } = new List<PracownikUprawnienie>();
}
