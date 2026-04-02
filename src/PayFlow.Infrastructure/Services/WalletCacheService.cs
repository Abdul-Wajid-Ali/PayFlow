using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Common.Models;
using System.Text.Json;

namespace PayFlow.Infrastructure.Services
{
    public class WalletCacheService : IWalletCacheService
    {
        private readonly ILogger<WalletCacheService> _logger;
        private readonly IDistributedCache _distributedCache;

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly TimeSpan BalanceCacheTtl = TimeSpan.FromSeconds(120 + Random.Shared.Next(0, 15));

        public WalletCacheService(ILogger<WalletCacheService> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task SetBalanceAsync(WalletCacheResult result, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1: Serialize DTO into JSON payload
                var payload = JsonSerializer.Serialize(result, SerializerOptions);

                // 2: Configure cache expiration
                var cacheEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = BalanceCacheTtl
                };

                var balanceKey = GetBalanceKey(result.WalletId);
                var walletIdKey = GetWalletIdLookupKey(userId);

                // 3: Store balance payload in cache
                await _distributedCache.SetStringAsync(balanceKey, payload, cacheEntryOptions, cancellationToken);
                _logger.LogInformation("CACHE SET for {BalanceKey}", balanceKey);

                // 4: Store userId → walletId mapping
                await _distributedCache.SetStringAsync(walletIdKey, result.WalletId.ToString(), cacheEntryOptions, cancellationToken);
                _logger.LogInformation("CACHE SET for {WalletIdKey}", walletIdKey);
            }
            catch (Exception ex)
            {
                // 5: Log cache set failure
                _logger.LogWarning(ex, "CACHE SET FAILURE for WalletBalance UserId {UserId}", userId);
            }
        }

        public async Task<WalletCacheResult?> TryGetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 1: Attempt to resolve walletId from cache
            var walletId = await TryGetWalletIdFromCacheAsync(userId, cancellationToken);

            // 2: Attempt to retrieve cached balance if walletId exists
            if (walletId is not null)
            {
                var cachedResult = await TryGetCachedBalanceAsync(walletId.Value, cancellationToken);

                // 3: Return cached value if found
                if (cachedResult is not null)
                {
                    _logger.LogInformation("CACHE HIT for WalletBalance UserId {UserId}", userId);

                    return cachedResult;
                }
            }

            _logger.LogInformation("DB HIT for WalletBalance UserId {UserId}", userId);

            return null;
        }

        private async Task<Guid?> TryGetWalletIdFromCacheAsync(Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                // 1: Fetch walletId using userId lookup key
                var walletIdKey = GetWalletIdLookupKey(userId);
                var walletIdValue = await _distributedCache.GetStringAsync(walletIdKey, cancellationToken);

                // 2: Validate and parse walletId
                if (Guid.TryParse(walletIdValue, out var walletId))
                {
                    _logger.LogInformation("CACHE LOOKUP HIT for {WalletIdKey} -> {WalletId}", walletIdKey, walletId);
                    return walletId;
                }

                _logger.LogInformation("CACHE MISS for walletId lookup {WalletIdKey}", walletIdKey);
                return null;
            }
            catch (Exception ex)
            {
                // 3: Log cache failure and fallback
                _logger.LogWarning(ex, "CACHE FAILURE during walletId lookup for UserId {UserId}", userId);
                return null;
            }
        }

        private async Task<WalletCacheResult?> TryGetCachedBalanceAsync(Guid walletId, CancellationToken cancellationToken)
        {
            try
            {
                // 1: Fetch cached balance payload
                var balanceKey = GetBalanceKey(walletId);
                var cachedPayload = await _distributedCache.GetStringAsync(balanceKey, cancellationToken);

                // 2: Check if payload exists
                if (!string.IsNullOrWhiteSpace(cachedPayload))
                {
                    _logger.LogInformation("CACHE HIT for {BalanceKey}", balanceKey);

                    // 3: Deserialize payload into DTO
                    var cachedBalance = JsonSerializer.Deserialize<WalletCacheResult>(cachedPayload, SerializerOptions);

                    // 4: Return if successful
                    if (cachedBalance is not null)
                    {
                        return cachedBalance;
                    }

                    _logger.LogWarning("CACHE DESERIALIZATION FAILED for {BalanceKey}", balanceKey);
                }
                else
                {
                    _logger.LogInformation("CACHE MISS for {BalanceKey}", balanceKey);
                }

                return null;
            }
            catch (Exception ex)
            {
                // 5: Log cache failure and fallback
                _logger.LogWarning(ex, "CACHE FAILURE during balance fetch for WalletId {WalletId}", walletId);
                return null;
            }
        }

        // Generates cache key for wallet balance
        private static string GetBalanceKey(Guid walletId) => $"wallet:balance:{walletId}";

        // Generates cache key for userId → walletId lookup
        private static string GetWalletIdLookupKey(Guid userId) => $"wallet:user:{userId}:walletId";
    }
}