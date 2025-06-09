using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Features.Idempotency;

namespace PaymentGateway.Infrastructure.Database.EntityConfigurations;

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        // Composite primary key
        builder.HasKey(x => new { x.Key, x.AppId, x.UserId });

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.AppId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Operation)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LinkedEntityId)
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Index for cleanup of old records
        builder.HasIndex(x => x.CreatedAt);
    }
}
