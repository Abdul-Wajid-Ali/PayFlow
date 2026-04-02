using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Wallet.DTOs;
using System.Text.Json;

namespace PayFlow.Infrastructure.Services;

public class WalletCacheService : IWalletCacheService
{
    private static readonly TimeSpan BalanceCacheTtl = TimeSpan.FromSeconds(120 + Random.Shared.Next(0, 15));
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<WalletCacheService> _logger;

    public WalletCacheService(IDistributedCache distributedCache, ILogger<WalletCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<WalletBalanceResponse?> TryGetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var walletIdKey = GetWalletIdLookupKey(userId);
            var walletIdValue = await _distributedCache.GetStringAsync(walletIdKey, cancellationToken);

            if (!Guid.TryParse(walletIdValue, out var walletId))
            {
                _logger.LogInformation("CACHE MISS for walletId lookup {WalletIdKey}", walletIdKey);
                return null;
            }

            _logger.LogInformation("CACHE LOOKUP HIT for {WalletIdKey} -> {WalletId}", walletIdKey, walletId);

            var balanceKey = GetBalanceKey(walletId);
            var cachedPayload = await _distributedCache.GetStringAsync(balanceKey, cancellationToken);

            if (string.IsNullOrWhiteSpace(cachedPayload))
            {
                _logger.LogInformation("CACHE MISS for {BalanceKey}", balanceKey);
                return null;
            }

            _logger.LogInformation("CACHE HIT for {BalanceKey}", balanceKey);

            var cachedBalance = JsonSerializer.Deserialize<WalletBalanceResponse>(cachedPayload, SerializerOptions);

            if (cachedBalance is null)
            {
                _logger.LogWarning("CACHE DESERIALIZATION FAILED for {BalanceKey}", balanceKey);
                return null;
            }

            return cachedBalance;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CACHE FAILURE during balance lookup for UserId {UserId}", userId);
            return null;
        }
    }

    public async Task SetBalanceAsync(WalletBalanceResponse balance, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(balance, SerializerOptions);

            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = BalanceCacheTtl
            };

            var balanceKey = GetBalanceKey(balance.WalletId);
            var walletIdKey = GetWalletIdLookupKey(userId);

            await _distributedCache.SetStringAsync(balanceKey, payload, cacheEntryOptions, cancellationToken);
            _logger.LogInformation("CACHE SET for {BalanceKey}", balanceKey);

            await _distributedCache.SetStringAsync(walletIdKey, balance.WalletId.ToString(), cacheEntryOptions, cancellationToken);
            _logger.LogInformation("CACHE SET for {WalletIdKey}", walletIdKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CACHE SET FAILURE for WalletBalance UserId {UserId}", userId);
        }
    }

    private static string GetBalanceKey(Guid walletId) => $"wallet:balance:{walletId}";

    private static string GetWalletIdLookupKey(Guid userId) => $"wallet:user:{userId}:walletId";
}
