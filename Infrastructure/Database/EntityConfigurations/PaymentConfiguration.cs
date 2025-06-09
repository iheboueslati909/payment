using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


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

        builder.Property(p => p.Provider)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.ProviderPaymentId)
            .HasMaxLength(100);

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Index for querying by AppId (tenant isolation)
        builder.HasIndex(p => p.AppId);
        
        // Index for AppId + UserId combination
        builder.HasIndex(p => new { p.AppId, p.UserId });
    }
}
