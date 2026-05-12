using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Krik.Api.Entities;
using Krik.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Krik.Api.Services;

public interface IJwtTokenService
{
    string CreateAccessToken(KrikUser user, IReadOnlyList<string> roleNames, IReadOnlyList<Guid> areaIds);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _opt = options.Value;

    public string CreateAccessToken(KrikUser user, IReadOnlyList<string> roleNames, IReadOnlyList<Guid> areaIds)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName)
        };

        foreach (var r in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, r));

        if (user.StoreId is { } storeId)
            claims.Add(new Claim("store_id", storeId.ToString()));

        foreach (var areaId in areaIds)
            claims.Add(new Claim("area_id", areaId.ToString()));

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opt.ExpiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
