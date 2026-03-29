using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.HasKey(om => om.Id);

            builder.Property(om => om.EventType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(om => om.Payload)
                .IsRequired();

            builder.Property(om => om.RoutingKey)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(om => om.LastError)
                .HasMaxLength(1000);

            // Index for the worker's poll query — only unprocessed, non-dead-lettered messages
            builder.HasIndex(om => new { om.ProcessedAt, om.DeadLetteredAt, om.NextRetryAt })
                .HasDatabaseName("IX_OutboxMessages_Pending");
        }
    }
}