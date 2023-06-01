public interface ISettingsService
{
    string GetUrl();
}

public class SettingsService : ISettingsService
{
    private readonly IConfiguration _configuration;

    public SettingsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetUrl() => _configuration.GetValue<string>("Url") ?? "https://localhost";
}