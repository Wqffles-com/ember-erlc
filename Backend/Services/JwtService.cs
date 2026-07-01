using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Backend.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public class JwtService(IOptions<JwtOptions> options, RsaSecurityKey signingKey) : IJwtService
{
    public const string NameIdentifierClaimType = ClaimTypes.NameIdentifier;

    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public static class RsaKeyLoader
{
    public static RsaSecurityKey LoadRsaKey(JwtOptions opts)
    {
        var rsa = RSA.Create();
        var ext = Path.GetExtension(opts.CertificatePath);

        if (string.Equals(ext, ".pfx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".p12", StringComparison.OrdinalIgnoreCase))
        {
            var password = string.IsNullOrEmpty(opts.CertificatePassword) ? null : opts.CertificatePassword;
            using var cert = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(opts.CertificatePath), password);
            rsa.ImportFromPem(cert.GetRSAPrivateKey()!.ExportRSAPrivateKeyPem());
        }
        else
        {
            var pem = File.ReadAllText(opts.CertificatePath);
            rsa.ImportFromPem(pem);
        }

        return new RsaSecurityKey(rsa);
    }
}
