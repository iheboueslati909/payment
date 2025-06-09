using Microsoft.EntityFrameworkCore;
using PaymentGateway.Features.Idempotency;
using PaymentGateway.Features.Payments.Models;
using PaymentGateway.Infrastructure.Database.EntityConfigurations;
using PaymentGateway.Infrastructure.Outbox;

namespace PaymentGateway.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public DbSet<Features.Payments.Models.Payment> Payments => Set<Features.Payments.Models.Payment>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}