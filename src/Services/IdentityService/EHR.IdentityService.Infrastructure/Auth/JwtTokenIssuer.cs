using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Application.Staff;
using EHR.IdentityService.Domain.Staff;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly IConfiguration _configuration;
    private readonly IStaffMetadataRepository _staffMetadata;

    public JwtTokenIssuer(IConfiguration configuration, IStaffMetadataRepository staffMetadata)
    {
        _configuration = configuration;
        _staffMetadata = staffMetadata;
    }

    public async Task<TokenResponse> IssueAsync(StaffUser staffUser, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var signingKey = _configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, staffUser.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, staffUser.Id.ToString()),
            new Claim("tenant_id", staffUser.TenantId),
            new Claim(ClaimTypes.Name, staffUser.FullName),
            new Claim(ClaimTypes.Email, staffUser.Email),
            new Claim(ClaimTypes.Role, staffUser.Role)
        };

        foreach (var permission in await _staffMetadata.GetPermissionsForRoleAsync(staffUser.TenantId, staffUser.Role, cancellationToken))
        {
            claims.Add(new Claim("permission", permission));
        }

        var jwt = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new TokenResponse(new JwtSecurityTokenHandler().WriteToken(jwt), CreateRefreshToken(), expiresAt);
    }

    private static string CreateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
