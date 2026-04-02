using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Constants;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Exceptions;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Events;
using PayFlow.Domain.Interfaces;
using System.Net;
using System.Text.Json;

namespace PayFlow.Application.Features.Auth.Commands
{
    public class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IUnitOfWork _unitOfWork;

        private readonly IPasswordService _passwordService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<RegisterCommandHandler> _logger;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IWalletRepository walletRepository,
            IOutboxRepository outboxRepository,
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IDateTimeProvider dateTimeProvider,
            ILogger<RegisterCommandHandler> logger)
        {
            _userRepository = userRepository;
            _walletRepository = walletRepository;
            _outboxRepository = outboxRepository;
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<RegisterResponse> Handle(
            RegisterCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Registration attempt for Email {Email}", command.Email);

            // 1. Check if email already exists and throw a BusinessRuleException if it does
            var existingUser = await _userRepository.ExistsAsync(command.Email, cancellationToken);
            if (existingUser)
            {
                _logger.LogWarning("Registration failed: email already exists for Email {Email}", command.Email);

                throw new BusinessRuleException(
                    title: "Email already exists.",
                    detail: $"A user with email '{command.Email}' already exists.",
                    statusCode: (int)HttpStatusCode.Conflict
                );
            }

            //2. Hash the password and generate salt
            var hashResult = _passwordService.Hash(command.Password);

            //3. Create User, Wallet, OutboxMessage entities and UserRegisteredEvent
            var newUser = User.Create(
                email: command.Email,
                passwordHash: hashResult.Hash,
                passwordSalt: hashResult.Salt,
                createAt: _dateTimeProvider.UtcNow);

            var newWallet = Domain.Entities.Wallet.Create(userId: newUser.Id);

            var @event = new UserRegisteredEvent(
                UserId: newUser.Id,
                WalletId: newWallet.Id,
                CreatedAt: newUser.CreatedAt);

            var newOutboxMessage = OutboxMessage.Create(
                eventType: nameof(UserRegisteredEvent),
                payload: JsonSerializer.Serialize(@event),
                routingKey: DomainEvents.UserRegistered,
                createdAt: _dateTimeProvider.UtcNow);

            //4. Save entities in a transaction
            await _userRepository.AddAsync(newUser, cancellationToken);
            await _walletRepository.AddAsync(newWallet, cancellationToken);
            await _outboxRepository.AddAsync(newOutboxMessage, cancellationToken);

            // 5: Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Registration successful. UserId {UserId}, Email {Email}, WalletId {WalletId}",
                newUser.Id, newUser.Email, newWallet.Id);

            //6: Return response DTO
            return new RegisterResponse
            (
                UserId: newUser.Id,
                Email: newUser.Email,
                WalletId: newWallet.Id
            );
        }
    }
}