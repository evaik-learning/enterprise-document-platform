namespace Edp.DigitalSignature.Infrastructure.Persistence;

using Edp.DigitalSignature.Domain.Entities;
using Edp.DigitalSignature.Infrastructure.Outbox;
using Edp.Shared.Security.CurrentUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core DbContext for Digital Signature service.
/// </summary>
public class SigningDbContext : DbContext
{
    private readonly Guid? _organizationId;

    public SigningDbContext(DbContextOptions<SigningDbContext> options, ICurrentOrganization? currentOrganization = null)
        : base(options)
    {
        _organizationId = currentOrganization?.OrganizationId;
    }

    public DbSet<SigningRequest> SigningRequests { get; set; } = null!;
    public DbSet<Signer> Signers { get; set; } = null!;
    public DbSet<SignatureField> SignatureFields { get; set; } = null!;
    public DbSet<SignatureAction> SignatureActions { get; set; } = null!;
    public DbSet<SigningProviderTransaction> ProviderTransactions { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSigningRequest(modelBuilder.Entity<SigningRequest>());
        ConfigureSigner(modelBuilder.Entity<Signer>());
        ConfigureSignatureField(modelBuilder.Entity<SignatureField>());
        ConfigureSignatureAction(modelBuilder.Entity<SignatureAction>());
        ConfigureSigningProviderTransaction(modelBuilder.Entity<SigningProviderTransaction>());

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(message => message.Id);
            builder.Property(message => message.EventType).HasMaxLength(200).IsRequired();
            builder.Property(message => message.AggregateType).HasMaxLength(200).IsRequired();
            builder.Property(message => message.Payload).IsRequired();
            builder.Property(message => message.Error).HasMaxLength(2000);
            builder.HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc });
        });

        modelBuilder.Entity<SigningRequest>().HasQueryFilter(request =>
            !_organizationId.HasValue || request.OrganizationId == _organizationId.Value);
    }

    private void ConfigureSigningRequest(EntityTypeBuilder<SigningRequest> builder)
    {
        builder.HasKey(sr => sr.SigningRequestId);

        builder.Property(sr => sr.DocumentHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(sr => sr.Provider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(sr => sr.ProviderRequestId)
            .HasMaxLength(256);

        builder.Property(sr => sr.RowVersion)
            .IsRowVersion();

        builder.HasMany(sr => sr.Signers)
            .WithOne(s => s.SigningRequest)
            .HasForeignKey(s => s.SigningRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sr => sr.OrganizationId);
        builder.HasIndex(sr => sr.WorkflowInstanceId);
        builder.HasIndex(sr => sr.DocumentId);
        builder.HasIndex(sr => sr.Status);
        builder.HasIndex(sr => sr.CreatedAt);
        builder.HasIndex(sr => sr.ExpiresAt);
    }

    private void ConfigureSigner(EntityTypeBuilder<Signer> builder)
    {
        builder.HasKey(s => s.SignerId);
    }

    private void ConfigureSignatureField(EntityTypeBuilder<SignatureField> builder)
    {
        builder.HasKey(sf => sf.SignatureFieldId);
    }

    private void ConfigureSignatureAction(EntityTypeBuilder<SignatureAction> builder)
    {
        builder.HasKey(sa => sa.SignatureActionId);
        builder.Property(sa => sa.OccurredAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }

    private void ConfigureSigningProviderTransaction(EntityTypeBuilder<SigningProviderTransaction> builder)
    {
        builder.HasKey(spt => spt.ProviderTransactionId);
        builder.HasIndex(spt => new { spt.SigningRequestId, spt.Provider, spt.ProviderRequestId })
            .IsUnique();
    }
}
