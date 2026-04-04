using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Constants;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Events;
using PayFlow.Domain.Interfaces;
using System.Net;
using System.Text.Json;

namespace PayFlow.Application.Features.Transfers.Commands
{
    public class TransferCommandHandlerV2 : ICommandHandler<TransferCommandV2, TransferAcceptedResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<TransferCommandHandlerV2> _logger;
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
            _outboxRepository = outboxRepository;
        }

        public async Task<TransferAcceptedResponse> Handle(TransferCommandV2 command, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Transfer initiated from {SenderUserId} to {ReceiverUserId} for {Amount} {Currency} with IdempotencyKey {IdempotencyKey}",
                command.SenderUserId, command.ReceiverUserId, command.Amount, command.Currency, command.IdempotencyKey);

            // 1: Load sender wallet and throw BusinessRuleException if not found
            var senderWallet = await _walletRepository.GetByUserIdAsync(command.SenderUserId, cancellationToken);
            if (senderWallet is null)
            {
                _logger.LogWarning("Transfer failed: sneder wallet not found for UserId {SenderUserId}", command.SenderUserId);

                throw new BusinessRuleException(
                    title: "Sender wallet not found.",
                    detail: "No wallet is associated with the sender account.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            //// 2: Load receiver wallet and throw BusinessRuleException if not found
            var receiverWallet = await _walletRepository.GetByUserIdAsync(command.ReceiverUserId, cancellationToken);
            if (receiverWallet is null)
            {
                _logger.LogWarning("Transfer failed: receiver wallet not found for UserId {ReceiverUserId}", command.ReceiverUserId);

                throw new BusinessRuleException(
                    title: "Receiver wallet not found.",
                    detail: "No wallet is associated with the receiver account.",
                    statusCode: (int)HttpStatusCode.NotFound);
            }

            // 3: Create transaction as pending
            var transaction = Transaction.Create(
                 fromWalletId: senderWallet.Id,
                 toWalletId: receiverWallet.Id,
                 amount: command.Amount,
                 currency: command.Currency,
                 idempotencyKey: command.IdempotencyKey,
                 createdAt: _dateTimeProvider.UtcNow
             );

            await _transactionRepository.AddAsync(transaction, cancellationToken);

            _logger.LogWarning("Transaction Created: Transaction with {TransactionId} with Status Pending.", transaction.Id);

            // 4: Create TransferRequestedEvent for Transfer Request Processing
            var transferRequestedEvent = new TransferRequestedEvent(
                TransactionId: transaction.Id,
                FromWalletId: senderWallet.Id,
                ToWalletId: receiverWallet.Id,
                Amount: command.Amount,
                Currency: command.Currency,
                IdempotencyKey: command.IdempotencyKey,
                CreatedAt: transaction.CreatedAt);

            // 5: Create Outbox Entity for Transfer Requested and commit to OutboxRepository
            await _outboxRepository.AddAsync(
                OutboxMessage.Create(
                    eventType: nameof(TransferRequestedEvent),
                    payload: JsonSerializer.Serialize(transferRequestedEvent),
                    routingKey: DomainEvents.TransferRequested,
                    createdAt: _dateTimeProvider.UtcNow),
                cancellationToken);

            _logger.LogWarning("Transaction Requested: Outbox event Transfer Requested created for {TransactionId}.", transaction.Id);

            // 6: Persist changes atomically
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new(
               TransactionId: transaction.Id,
               Status: transaction.Status
           );
        }
    }
}