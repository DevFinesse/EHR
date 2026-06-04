using System.Collections.Concurrent;
using EHR.IdentityService.Application.Auth;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<string, RefreshTokenRecord> _tokens = new();

    public Task StoreAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken)
    {
        _tokens[refreshToken.Token] = refreshToken;
        return Task.CompletedTask;
    }

    public Task<RefreshTokenRecord?> GetAsync(string token, CancellationToken cancellationToken)
    {
        _tokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        if (_tokens.TryGetValue(token, out var existing))
        {
            _tokens[token] = existing with { RevokedAt = DateTimeOffset.UtcNow };
        }

        return Task.CompletedTask;
    }
}
