using ICFAccounting.Data.Configurations;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ICFAccounting.Data;

/// <summary>
/// Maps accounting aggregates to PostgreSQL without exposing persistence concerns to the domain.
/// </summary>
public sealed class ICFAccountingDbContext : DbContext
{
    /// <summary>
    /// Creates a context using externally configured database options.
    /// </summary>
    public ICFAccountingDbContext(DbContextOptions<ICFAccountingDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the pledgers.</summary>
    public DbSet<Pledger> Pledgers => Set<Pledger>();

    /// <summary>Gets the pledge categories.</summary>
    public DbSet<PledgeCategory> PledgeCategories => Set<PledgeCategory>();

    /// <summary>Gets the pledges.</summary>
    public DbSet<Pledge> Pledges => Set<Pledge>();

    /// <summary>Gets the pledge payments.</summary>
    public DbSet<PledgePayment> PledgePayments => Set<PledgePayment>();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        try
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (DbUpdateException exception) when (IsRelationshipConflict(exception))
        {
            throw new RelationshipConflictException(exception);
        }
    }

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsRelationshipConflict(exception))
        {
            throw new RelationshipConflictException(exception);
        }
    }

    private bool IsRelationshipConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException
            || postgresException.SqlState != PostgresErrorCodes.ForeignKeyViolation
            || postgresException.ConstraintName is not { } constraintName)
        {
            return false;
        }

        var foreignKeyNames = Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys())
            .Select(foreignKey => foreignKey.GetConstraintName()).OfType<string>().ToArray();
        return foreignKeyNames.Contains(constraintName, StringComparer.Ordinal);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.HasCollation(
            "icf_case_insensitive_accent_sensitive",
            locale: "und-u-ks-level2",
            provider: "icu",
            deterministic: false);
        modelBuilder.ApplyConfiguration(new PledgerConfiguration());
        modelBuilder.ApplyConfiguration(new PledgeCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new PledgeConfiguration());
        modelBuilder.ApplyConfiguration(new PledgePaymentConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().UseCollation("icf_case_insensitive_accent_sensitive");
    }
}