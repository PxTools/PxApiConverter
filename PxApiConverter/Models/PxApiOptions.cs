namespace PxApiConverter.Models;

public class PxApiOptions
{
    // URL prefix for test environment
    public string? TestUrlPrefix { get; set; }
    // URL prefix for production environment
    public string? ProdUrlPrefix { get; set; }

    // (Optional) Could be used later if you want to switch active prefix via config
    public string? ActiveUrlPrefix { get; set; }
}
