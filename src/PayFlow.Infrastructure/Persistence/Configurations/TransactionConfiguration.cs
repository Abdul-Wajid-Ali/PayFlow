using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(t => t.FromWalletId)
                .IsRequired();

            builder.Property(t => t.ToWalletId)
                .IsRequired();

            builder.Property(t => t.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.Currency)
                .IsRequired()
                .HasMaxLength(3); // ISO 4217

            builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>() // Store as "Pending" / "Completed" / "Failed"
            .HasMaxLength(32);

            builder.Property(t => t.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(256);

            builder.HasIndex(t => t.IdempotencyKey)
                .IsUnique(); // Core guarantee — no duplicate transactions

            builder.Property(t => t.CreatedAt)
                .IsRequired();
        }
    }
}