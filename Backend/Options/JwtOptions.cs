namespace Backend.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
