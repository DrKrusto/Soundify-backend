public class JwtModel
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationHours { get; set; }
    public string SecretKey { get; set; }
}