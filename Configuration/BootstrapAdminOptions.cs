namespace WebAPI.Configuration;

public class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Login { get; set; } = "admin";
    public string Password { get; set; } = null!;
    public string Imie { get; set; } = "System";
    public string Nazwisko { get; set; } = "Administrator";
    public string FirmaNazwa { get; set; } = "Firma systemowa";
    public string FirmaNip { get; set; } = "0000000000";
    public string DzialNazwa { get; set; } = "Administracja";
    public string StanowiskoNazwa { get; set; } = "Administrator";
}
