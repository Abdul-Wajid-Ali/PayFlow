using PayFlow.Application.Common.Features.Auth.DTOs;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;

namespace PayFlow.Application.Common.Features.Auth.Commands
{
    public class RegisterCommandHandler
    {
        private readonly IUserRepository _userRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IWalletRepository walletRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegisterResponse> HandleAsync(
            RegisterCommand command,
            CancellationToken cancellationToken = default)
        {
            // 1. Guard — email must be unique
            var existingUser = await _userRepository.ExistsAsync(command.Email);
            if (existingUser)
                throw new InvalidOperationException($"Email '{command.Email}' is already registered.");

            //2. Password hashing
            var (passwordHash, passwordSalt) = _passwordHasher.Hash(command.Password);

            //3. Create domain objects
            var newUser = User.Create(command.Email, passwordHash, passwordSalt);
            var newWallet = Wallet.Create(newUser.Id);

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