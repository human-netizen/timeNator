using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TimeNator.Api.Data;
using TimeNator.Api.Entities;

namespace TimeNator.Api.Services;

public record IssuedTokens(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken);

public class TokenService(AppDbContext db, IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly JwtOptions _options = options.Value;

    public async Task<IssuedTokens> IssueAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var (accessToken, accessExpires) = CreateAccessToken(user, now);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            ExpiresAt = now.AddDays(_options.RefreshTokenDays),
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);

        return new IssuedTokens(accessToken, accessExpires, refreshToken);
    }

    public async Task<(ApplicationUser User, IssuedTokens Tokens)?> RefreshAsync(
        string refreshToken, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= now)
            return null;

        var (accessToken, accessExpires) = CreateAccessToken(stored.User, now);
        return (stored.User, new IssuedTokens(accessToken, accessExpires, refreshToken));
    }

    public Task RevokeAsync(Guid userId, string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenHash == hash && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, clock.GetUtcNow()), cancellationToken);
    }

    private (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(ApplicationUser user, DateTimeOffset now)
    {
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
