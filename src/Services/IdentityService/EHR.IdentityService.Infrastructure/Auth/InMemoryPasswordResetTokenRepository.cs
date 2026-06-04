using System.Collections.Concurrent;
using EHR.IdentityService.Application.Auth;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ConcurrentDictionary<string, PasswordResetTokenRecord> _tokens = new();

    public Task StoreAsync(PasswordResetTokenRecord resetToken, CancellationToken cancellationToken)
    {
        _tokens[resetToken.Token] = resetToken;
        return Task.CompletedTask;
    }

    public Task<PasswordResetTokenRecord?> GetAsync(string token, CancellationToken cancellationToken)
    {
        _tokens.TryGetValue(token, out var resetToken);
        return Task.FromResult(resetToken);
    }

    public Task MarkUsedAsync(string token, CancellationToken cancellationToken)
    {
        if (_tokens.TryGetValue(token, out var resetToken))
        {
            _tokens[token] = resetToken with { UsedAt = DateTimeOffset.UtcNow };
        }

        return Task.CompletedTask;
    }
}
