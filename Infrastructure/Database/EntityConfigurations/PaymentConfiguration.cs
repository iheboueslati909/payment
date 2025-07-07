using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Features.Payments.Models;


namespace PaymentGateway.Infrastructure.Database.EntityConfigurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Features.Payments.Models.Payment>
{
    public void Configure(EntityTypeBuilder<Features.Payments.Models.Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.AppId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        // IntendId (should be required, max length)
        builder.Property(p => p.IntendId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Status)
            .IsRequired();

        // IdempotencyKey (should be required, max length)
        builder.Property(p => p.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        // CheckoutUrl (optional, max length)
        builder.Property(p => p.CheckoutUrl)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.Provider)
            .IsRequired();

        // Index for querying by AppId (tenant isolation)
        builder.HasIndex(p => p.AppId);

        // Index for AppId + UserId combination
        builder.HasIndex(p => new { p.AppId, p.UserId });

        // Unique index for idempotency (AppId + UserId + IdempotencyKey)
        builder.HasIndex(p => new { p.AppId, p.UserId, p.IdempotencyKey }).IsUnique();
    }
}