using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .IsRequired()
                .ValueGeneratedNever(); // We generate Guid in domain, not DB

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                .IsUnique(); // Email must be unique across all users

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(u => u.PasswordSalt)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(u => u.Status)
                .IsRequired()
                .HasConversion<string>() // Store as "Active" / "Suspended" not 0 / 1
                .HasMaxLength(32);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            // One user owns exactly one wallet
            builder.HasOne(u => u.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Never cascade delete financial data
        }
    }
}