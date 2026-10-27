using ICFAccounting.Data;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ICFAccounting.IntegrationTests.Repositories;

/// <summary>
/// Provides isolated database lifecycle and related-record setup for SQL repository tests.
/// </summary>
public abstract class SqlServerRepositoryTestBase(SqlServerModelFixture fixture) : IAsyncLifetime
{
    private readonly string _databaseName = $"ICFAccountingRepositoryTests_{Guid.NewGuid():N}";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    protected ICFAccountingDbContext CreateContext()
    {
        return fixture.CreateContext(_databaseName);
    }

    protected async Task<(Pledger Pledger, PledgeCategory Category)> SeedParentsAsync()
    {
        var pledger = Pledger.Create("Test pledger");
        var category = PledgeCategory.Create("Test category");
        await using var context = CreateContext();
        context.AddRange(pledger, category);
        await context.SaveChangesAsync();
        return (pledger, category);
    }

    protected async Task<Pledge> SeedPledgeAsync()
    {
        var (pledger, category) = await SeedParentsAsync();
        var pledge = Pledge.Create(2026, 100m, pledger.Id, category.Id);
        await using var context = CreateContext();
        context.Pledges.Add(pledge);
        await context.SaveChangesAsync();
        return pledge;
    }

    protected async Task<(Pledge Pledge, PledgePayment Payment)> SeedPaymentAsync()
    {
        var pledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
        await using var context = CreateContext();
        await CreatePaymentRepository(context).AddAsync(payment);
        return (pledge, payment);
    }

    protected static SqlServerPledgePaymentRepository CreatePaymentRepository(ICFAccountingDbContext context)
    {
        return new SqlServerPledgePaymentRepository(context, NullLogger<SqlServerPledgePaymentRepository>.Instance);
    }

    protected async Task AssertRelatedRecordsExistAsync(Pledge pledge, PledgePayment payment)
    {
        await using var context = CreateContext();
        Assert.NotNull(await new SqlServerPledgerRepository(context).GetByIdAsync(pledge.PledgerId));
        Assert.NotNull(await new SqlServerPledgeCategoryRepository(context).GetByIdAsync(pledge.PledgeCategoryId));
        Assert.NotNull(await new SqlServerPledgeRepository(context).GetByIdAsync(pledge.Id));
        Assert.NotNull(await CreatePaymentRepository(context).GetByIdAsync(payment.Id));
    }

    protected async Task RemoveRelatedRecordsAsync(Pledge pledge, PledgePayment payment)
    {
        await using var context = CreateContext();
        await CreatePaymentRepository(context).RemoveAsync(payment.Id);
        await new SqlServerPledgeRepository(context).RemoveAsync(pledge.Id);
        await new SqlServerPledgerRepository(context).RemoveAsync(pledge.PledgerId);
        await new SqlServerPledgeCategoryRepository(context).RemoveAsync(pledge.PledgeCategoryId);
    }
}