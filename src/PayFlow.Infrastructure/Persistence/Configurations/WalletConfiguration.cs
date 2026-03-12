using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.ToTable("Wallets");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(w => w.UserId)
                .IsRequired();

            builder.Property(w => w.Currency)
                .IsRequired()
                .HasMaxLength(3); // ISO 4217 currency codes e.g. USD, EUR, PKR

            builder.Property(w => w.Balance)
                .IsRequired()
                .HasColumnType("decimal(18,2)") // 18 digits, 2 decimal places
                .HasDefaultValue(0.00m);

            // Outgoing transactions from this wallet
            builder.HasMany(w => w.OutgoingTransactions)
                .WithOne(t => t.FromWallet)
                .HasForeignKey(t => t.FromWalletId)
                .OnDelete(DeleteBehavior.Restrict);

            // Incoming transactions to this wallet
            builder.HasMany(w => w.IncomingTransactions)
                .WithOne(t => t.ToWallet)
                .HasForeignKey(t => t.ToWalletId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}