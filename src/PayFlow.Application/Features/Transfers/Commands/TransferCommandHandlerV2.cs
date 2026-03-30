using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Constants;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Events;
using System.Net;
using System.Text.Json;

namespace PayFlow.Application.Features.Transfers.Commands
{
    public class TransferCommandHandlerV2 : ICommandHandler<TransferCommandV2, TransferResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<TransferCommandHandlerV2> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOutboxRepository _outboxRepository;

        public TransferCommandHandlerV2(
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IWalletRepository walletRepository,
            ITransactionRepository transactionRepository,
            ILogger<TransferCommandHandlerV2> logger,
            ICurrentUserService currentUserService,
            IOutboxRepository outboxRepository)
        {
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;
            _currentUserService = currentUserService;
            _outboxRepository = outboxRepository;
        }

        public async Task<TransferResponse> Handle(TransferCommandV2 command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Transfer initiated from {SenderUserId} to {ReceiverUserId} for {Amount} {Currency} with IdempotencyKey {IdempotencyKey}",
                command.SenderUserId, command.ReceiverUserId, command.Amount, command.Currency, command.IdempotencyKey);

            //1: Load sender wallet from authenticated user context
            var senderWallet = await _walletRepository.GetByUserIdAsync(_currentUserService.UserId, cancellationToken);
            if (senderWallet is null)
            {
                _logger.LogWarning("V2 transfer failed: sender wallet not found for UserId {SenderUserId}", command.SenderUserId);

                throw new BusinessRuleException(
                    title: "Sender wallet not found.",
                    detail: "No wallet is associated with the sender account.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            //2: Check for duplicate transaction scoped to sender wallet
            var existing = await _transactionRepository.GetByIdempotencyKeyAndWalletIdAsync(command.IdempotencyKey, senderWallet.Id, cancellationToken);
            if (existing is not null)
            {
                if (existing.FromWalletId == command.SenderUserId)
                {
                    _logger.LogInformation(
                        "V2 duplicate transfer detected for IdempotencyKey {IdempotencyKey}, returning existing TransactionId {TransactionId}",
                        command.IdempotencyKey,
                        existing.Id);

                    return MapToResponse(existing);
                }

                //3: Reject if idempotency key is claimed by a different wallet
                throw new IdempotencyConflictException("The provided idempotency key was already used by a different user.");
            }

            //4: Cross-check idempotency key globally to detect cross-user key reuse
            var existingByKey = await _transactionRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
            if (existingByKey is not null && existingByKey.FromWalletId != command.SenderUserId)
                throw new IdempotencyConflictException("The provided idempotency key was already used by a different user.");

            //5: Load receiver wallet and throw BusinessRuleException if not found
            var receiverWallet = await _walletRepository.GetByUserIdAsync(command.ReceiverUserId, cancellationToken);
            if (receiverWallet is null)
            {
                _logger.LogWarning("Transfer failed: receiver wallet not found for UserId {ReceiverUserId}", command.ReceiverUserId);

                throw new BusinessRuleException(
                    title: "Receiver wallet not found.",
                    detail: "No wallet is associated with the receiver account.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            //6: Validate transfer currency matches both wallet currencies
            if (senderWallet.Currency != command.Currency || receiverWallet.Currency != command.Currency)
            {
                _logger.LogWarning(
                    "Transfer failed: currency mismatch. Transfer={TransferCurrency}, Sender={SenderCurrency}, Receiver={ReceiverCurrency}",
                    command.Currency, senderWallet.Currency, receiverWallet.Currency);

                throw new BusinessRuleException(
                    title: "Currency mismatch.",
                    detail: "Transfer currency must match both sender and receiver wallet currencies.",
                    statusCode: (int)HttpStatusCode.BadRequest);
            }

            //7: Reject self-transfer if sender and receiver resolve to the same wallet
            if (senderWallet.Id == receiverWallet.Id)
            {
                _logger.LogWarning("Transfer failed: self-transfer attempted by WalletId {WalletId}", senderWallet.Id);

                throw new BusinessRuleException(
                    title: "Invalid transfer.",
                    detail: "Sender and receiver cannot be the same wallet.",
                    statusCode: (int)HttpStatusCode.BadRequest);
            }

            //8: Verify sender has sufficient balance
            if (senderWallet.Balance < command.Amount)
            {
                _logger.LogWarning(
                    "Transfer failed: insufficient balance. WalletId {WalletId}, Balance {Balance}, Amount {Amount} {Currency}",
                    senderWallet.Id, senderWallet.Balance, command.Amount, command.Currency);

                throw new BusinessRuleException(
                     title: "Insufficient balance.",
                     detail: $"Available balance {senderWallet.Balance} {senderWallet.Currency} is less than transfer amount {command.Amount}.",
                     statusCode: (int)HttpStatusCode.UnprocessableEntity);
            }

            //9: Create transaction as pending
            var transaction = Transaction.Create(
                 fromWalletId: senderWallet.Id,
                 toWalletId: receiverWallet.Id,
                 amount: command.Amount,
                 currency: command.Currency,
                 idempotencyKey: command.IdempotencyKey,
                 createdAt: _dateTimeProvider.UtcNow
             );

            await _transactionRepository.AddAsync(transaction, cancellationToken);

            //10: Debit amount from sender wallet
            senderWallet.Debit(command.Amount);

            // 10a: Create WalletBalanceChangedEvent for Sender Wallet
            var senderBalanceEvent = new WalletBalanceChangedEvent(
                WalletId: senderWallet.Id,
                UserId: senderWallet.UserId,
                NewBalance: senderWallet.Balance,
                Currency: senderWallet.Currency,
                UpdatedAt: _dateTimeProvider.UtcNow);

            // 10b: Create Outbox Entity for Sender Wallet and commit to OutboxRepository
            await _outboxRepository.AddAsync(OutboxMessage.Create(
                eventType: nameof(WalletBalanceChangedEvent),
                payload: JsonSerializer.Serialize(senderBalanceEvent),
                routingKey: DomainEvents.WalletBalanceChanged,
                createdAt: _dateTimeProvider.UtcNow), cancellationToken);

            //11: Credit amount to receiver wallet
            receiverWallet.Credit(command.Amount);

            // 11a: Create WalletBalanceChangedEvent for Receiver Wallet
            var receiverBalanceEvent = new WalletBalanceChangedEvent(
                WalletId: receiverWallet.Id,
                UserId: receiverWallet.UserId,
                NewBalance: receiverWallet.Balance,
                Currency: receiverWallet.Currency,
                UpdatedAt: _dateTimeProvider.UtcNow);

            // 11b: Create Outbox Entity for Receiver Wallet and commit to OutboxRepository
            await _outboxRepository.AddAsync(OutboxMessage.Create(
                eventType: nameof(WalletBalanceChangedEvent),
                payload: JsonSerializer.Serialize(receiverBalanceEvent),
                routingKey: DomainEvents.WalletBalanceChanged,
                createdAt: _dateTimeProvider.UtcNow), cancellationToken);

            //12: Mark transaction as completed
            transaction.MarkCompleted();

            //13: Persist changes atomically
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transfer completed successfully. TransactionId {TransactionId}, From {FromWalletId} to {ToWalletId}, Amount {Amount} {Currency}",
                transaction.Id, transaction.FromWalletId, transaction.ToWalletId, transaction.Amount, transaction.Currency);

            return MapToResponse(transaction);
        }

        private static TransferResponse MapToResponse(Transaction transaction) =>
           new(
               TransactionId: transaction.Id,
               FromWalletId: transaction.FromWalletId,
               ToWalletId: transaction.ToWalletId,
               Amount: transaction.Amount,
               Currency: transaction.Currency,
               Status: transaction.Status,
               CreatedAt: transaction.CreatedAt
           );
    }
}