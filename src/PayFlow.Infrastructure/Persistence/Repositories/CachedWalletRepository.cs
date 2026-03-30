using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Wallet.DTOs;
using PayFlow.Domain.Entities;
using System.Text.Json;

namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class CachedWalletRepository : IWalletRepository
    {
        private static readonly TimeSpan BalanceCacheTtl = TimeSpan.FromSeconds(120 + Random.Shared.Next(0, 15));
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly WalletRepository _innerRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<CachedWalletRepository> _logger;

        public CachedWalletRepository(
            WalletRepository innerRepository,
            IDistributedCache distributedCache,
            ILogger<CachedWalletRepository> logger)
        {
            _innerRepository = innerRepository;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(wallet, cancellationToken);

        public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _innerRepository.GetByUserIdAsync(userId, cancellationToken);

        public async Task<WalletBalanceResponse?> GetBalanceDtoByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 1: Attempt to resolve walletId from cache
            var walletId = await TryGetWalletIdFromCacheAsync(userId, cancellationToken);

            // 2: Attempt to retrieve cached balance if walletId exists
            if (walletId is not null)
            {
                var cached = await TryGetCachedBalanceAsync(walletId.Value, cancellationToken);

                // 3: Return cached value if found
                if (cached is not null)
                {
                    return cached;
                }
            }

            // 4: Fetch wallet balance from database (source of truth)
            var balanceDto = await GetBalanceFromDbAsync(userId, cancellationToken);

            // 5: Return null if wallet does not exist
            if (balanceDto is null)
            {
                return null;
            }

            // 6: Cache fresh data for future requests
            await CacheBalanceAsync(balanceDto, userId, cancellationToken);

            // 7: Return result
            return balanceDto;
        }

        public async Task UpdateBalanceCacheAsync(Wallet wallet, CancellationToken cancellationToken = default)
        {
            // 1: Create BalanceDTO later to be passed to set in cache.
            var balanceDto = new WalletBalanceResponse(
                WalletId: wallet.Id,
                UserId: wallet.UserId,
                Balance:wallet.Balance,
                Currency: wallet.Currency);

            // 2: Set the DTO in redis cache for later use.
            await CacheBalanceAsync(balanceDto, wallet.UserId, cancellationToken);
        }

        // Generates cache key for wallet balance
        private static string GetBalanceKey(Guid walletId) => $"wallet:balance:{walletId}";

        // Generates cache key for userId → walletId lookup
        private static string GetWalletIdLookupKey(Guid userId) => $"wallet:user:{userId}:walletId";

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

        private async Task<WalletBalanceResponse?> TryGetCachedBalanceAsync(Guid walletId, CancellationToken cancellationToken)
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
                    var cachedBalance = JsonSerializer.Deserialize<WalletBalanceResponse>(cachedPayload, SerializerOptions);

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

        private async Task<WalletBalanceResponse?> GetBalanceFromDbAsync(Guid userId, CancellationToken cancellationToken)
        {
            // 1: Fetch from database
            _logger.LogInformation("DB HIT for WalletBalance UserId {UserId}", userId);

            var balanceDto = await _innerRepository.GetBalanceDtoByUserIdAsync(userId, cancellationToken);

            // 2: Log null result
            if (balanceDto is null)
            {
                _logger.LogWarning("DB RESULT NULL for WalletBalance UserId {UserId}", userId);
            }

            return balanceDto;
        }

        private async Task CacheBalanceAsync(WalletBalanceResponse balanceDto, Guid userId, CancellationToken cancellationToken)
        {
            try
            {
                // 1: Serialize DTO into JSON payload
                var payload = JsonSerializer.Serialize(balanceDto, SerializerOptions);

                // 2: Configure cache expiration
                var cacheEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = BalanceCacheTtl
                };

                var balanceKey = GetBalanceKey(balanceDto.WalletId);
                var walletIdKey = GetWalletIdLookupKey(userId);

                // 3: Store balance payload in cache
                await _distributedCache.SetStringAsync(balanceKey, payload, cacheEntryOptions, cancellationToken);
                _logger.LogInformation("CACHE SET for {BalanceKey}", balanceKey);

                // 4: Store userId → walletId mapping
                await _distributedCache.SetStringAsync(walletIdKey, balanceDto.WalletId.ToString(), cacheEntryOptions, cancellationToken);
                _logger.LogInformation("CACHE SET for {WalletIdKey}", walletIdKey);
            }
            catch (Exception ex)
            {
                // 5: Log cache set failure
                _logger.LogWarning(ex, "CACHE SET FAILURE for WalletBalance UserId {UserId}", userId);
            }
        }
    }
}