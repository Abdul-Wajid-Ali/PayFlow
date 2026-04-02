using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Wallet.DTOs;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Repositories;

public class CachedWalletRepository : IWalletRepository
{
    private readonly WalletRepository _innerRepository;
    private readonly IWalletCacheService _cacheService;
    private readonly ILogger<CachedWalletRepository> _logger;

    public CachedWalletRepository(
        WalletRepository innerRepository,
        IWalletCacheService cacheService,
        ILogger<CachedWalletRepository> logger)
    {
        _innerRepository = innerRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
        => _innerRepository.AddAsync(wallet, cancellationToken);

    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _innerRepository.GetByUserIdAsync(userId, cancellationToken);

    public async Task<WalletBalanceResponse?> GetBalanceDtoByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // 1: Try cache first
        var cached = await _cacheService.TryGetBalanceAsync(userId, cancellationToken);
        if (cached is not null)
            return cached;

        // 2: Fallback to database (source of truth)
        _logger.LogInformation("DB HIT for WalletBalance UserId {UserId}", userId);
        var balanceDto = await _innerRepository.GetBalanceDtoByUserIdAsync(userId, cancellationToken);

        if (balanceDto is null)
        {
            _logger.LogWarning("DB RESULT NULL for WalletBalance UserId {UserId}", userId);
            return null;
        }

        // 3: Populate cache for future requests
        await _cacheService.SetBalanceAsync(balanceDto, userId, cancellationToken);

        return balanceDto;
    }
}
