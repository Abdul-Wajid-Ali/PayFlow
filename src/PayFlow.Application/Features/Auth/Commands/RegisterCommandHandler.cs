using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Domain.Entities;
using System.Net;

namespace PayFlow.Application.Features.Auth.Commands
{
    public class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IPasswordService _passwordService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IWalletRepository walletRepository,
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IDateTimeProvider dateTimeProvider)
        {
            _userRepository = userRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<RegisterResponse> HandleAsync(
            RegisterCommand command,
            CancellationToken cancellationToken = default)
        {
            // 1. Check if email already exists and throw a BusinessRuleException if it does
            var existingUser = await _userRepository.ExistsAsync(command.Email, cancellationToken);
            if (existingUser)
                throw new BusinessRuleException(
                    title: "Email already exists.",
                    detail: $"A user with email '{command.Email}' already exists.",
                    statusCode: (int)HttpStatusCode.Conflict
                );

            //2. Hash the password and generate salt
            var hashResult = _passwordService.Hash(command.Password);

            //3. Create domain objects
            var newUser = User.Create(command.Email, hashResult.Hash, hashResult.Salt, _dateTimeProvider.UtcNow);
            var newWallet = PayFlow.Domain.Entities.Wallet.Create(newUser.Id);

            //4: Persist both in a single transaction
            await _userRepository.AddAsync(newUser, cancellationToken);
            await _walletRepository.AddAsync(newWallet, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            //5: Return response DTO
            return new RegisterResponse
            (
                UserId: newUser.Id,
                Email: newUser.Email,
                WalletId: newWallet.Id
            );
        }
    }
}