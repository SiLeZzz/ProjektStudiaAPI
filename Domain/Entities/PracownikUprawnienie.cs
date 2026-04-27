namespace WebAPI.Domain.Entities;

public class PracownikUprawnienie
{
    public long PracownikId { get; set; }
    public Pracownik Pracownik { get; set; } = null!;

    public string SygnaturaUprawnienia { get; set; } = null!;
    public Uprawnienie Uprawnienie { get; set; } = null!;

    public DateOnly DataNadania { get; set; }
    public DateOnly? WazneDo { get; set; }
}
