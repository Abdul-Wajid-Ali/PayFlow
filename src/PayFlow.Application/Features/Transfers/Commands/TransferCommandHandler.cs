using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Transfers.DTOs;
using PayFlow.Domain.Entities;
using System.Net;

namespace PayFlow.Application.Features.Transfers.Commands
{
    public class TransferCommandHandler : ICommandHandler<TransferCommand, TransferResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;

        public TransferCommandHandler(
            IUnitOfWork unitOfWork,
            IWalletRepository walletRepository,
            ITransactionRepository transactionRepository)
        {
            _unitOfWork = unitOfWork;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<TransferResponse> HandleAsync(TransferCommand command, CancellationToken cancellationToken = default)
        {
            //1: Check if transaction with the same idempotency key exists
            var existing = await _transactionRepository.GetByIdempotencyKeyAsync(command.IdempotencyKey);
            if (existing is not null)
                return MapToResponse(existing);

            //2: Load sender wallet and throw BusinessRuleException if not found
            var senderWallet = await _walletRepository.GetByUserIdAsync(command.SenderUserId);
            if (senderWallet is null)
                throw new BusinessRuleException(
                    title: "Sender wallet not found.",
                    detail: "No wallet is associated with the sender account.",
                    statusCode: (int)HttpStatusCode.NotFound);

            //3: Load receiver wallet and throw BusinessRuleException if not found
            var receiverWallet = await _walletRepository.GetByUserIdAsync(command.RecieverUserId);
            if (receiverWallet is null)
                throw new BusinessRuleException(
                    title: "Reciever wallet not found.",
                    detail: "No wallet is associated with the Reciever account.",
                    statusCode: (int)HttpStatusCode.NotFound);

            //4: Check if sender and receiver wallets are the same then throw BusinessRuleException
            if (senderWallet.Id == receiverWallet.Id)
                throw new BusinessRuleException(
                    title: "Invalid transfer.",
                    detail: "Sender and receiver cannot be the same wallet.",
                    statusCode: (int)HttpStatusCode.BadRequest);

            //5: Check if sender wallet has sufficient balance then throw BusinessRuleException
            if (senderWallet.Balance < command.Amount)
                throw new BusinessRuleException(
                     title: "Insufficient balance.",
                     detail: $"Available balance {senderWallet.Balance} {senderWallet.Currency} is less than transfer amount {command.Amount}.",
                     statusCode: (int)HttpStatusCode.UnprocessableEntity);

            //6: Create transaction as pending
            var transaction = Transaction.Create(
                 fromWalletId: senderWallet.Id,
                 toWalletId: receiverWallet.Id,
                 amount: command.Amount,
                 currency: command.Currency,
                 idempotencyKey: command.IdempotencyKey
             );

            await _transactionRepository.AddAsync(transaction);

            //7: Dedcut/Debit amount from sender wallet
            senderWallet.Debit(command.Amount);

            //8: Add/Credit amount to receiver wallet
            receiverWallet.Credit(command.Amount);

            //9: Mark transaction as completed
            transaction.MarkCompleted();

            //10: Presist changes atomically
            await _unitOfWork.SaveChangesAsync();

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