using System.Data.SqlTypes;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace ICFAccounting.IntegrationTests.Repositories;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlServerPledgeRepositoryTests(SqlServerModelFixture fixture) : SqlServerRepositoryTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pledge_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var (pledger, category) = await SeedParentsAsync();
        var pledge = Pledge.Create(2025, 50m, pledger.Id, category.Id);
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(pledge);
            }
            else
            {
                await repository.AddAsync(pledge);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            var stored = await repository.GetByIdAsync(pledge.Id);
            Assert.NotNull(stored);
            stored.Update(2026, 100m, pledger.Id, category.Id, "After");
            await repository.AddAsync(stored);
            stored.Update(2027, decimal.MaxValue, pledger.Id, category.Id, "Final");
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            var stored = await repository.GetByIdAsync(pledge.Id);
            Assert.NotNull(stored);
            Assert.Equal(2027, stored.Year);
            Assert.Equal(decimal.MaxValue, stored.Amount);
            Assert.Equal("Final", stored.Comment);
            Assert.Equal(pledge.CreatedAt, stored.CreatedAt);
            await repository.RemoveAsync(pledge.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = new SqlServerPledgeRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(pledge.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(pledge.Id));
    }

    [Theory]
    [InlineData(false, PledgeSortBy.Year, SortOrder.Ascending)]
    [InlineData(false, PledgeSortBy.Year, SortOrder.Descending)]
    [InlineData(false, PledgeSortBy.Amount, SortOrder.Ascending)]
    [InlineData(false, PledgeSortBy.Amount, SortOrder.Descending)]
    [InlineData(true, PledgeSortBy.Year, SortOrder.Ascending)]
    [InlineData(true, PledgeSortBy.Year, SortOrder.Descending)]
    [InlineData(true, PledgeSortBy.Amount, SortOrder.Ascending)]
    [InlineData(true, PledgeSortBy.Amount, SortOrder.Descending)]
    public async Task Pledge_queries_filter_sort_and_page_in_sql(
        bool byCategory, PledgeSortBy sortBy, SortOrder sortOrder)
    {
        var (pledger, category) = await SeedParentsAsync();
        var (otherPledger, otherCategory) = await SeedParentsAsync();
        var amounts = new[] { 10m, decimal.MaxValue, 2m, 10m, 0.01m };
        var pledges = amounts.Select((amount, index) =>
            Pledge.Create(2025 + index % 2, amount, pledger.Id, category.Id)).ToList();
        pledges.Add(Pledge.Create(2026, 50m, pledger.Id, otherCategory.Id));
        pledges.Add(Pledge.Create(2026, 60m, otherPledger.Id, category.Id));
        pledges.Add(Pledge.Create(2026, 70m, otherPledger.Id, otherCategory.Id));
        await using (var context = CreateContext())
        {
            context.Pledges.AddRange(pledges);
            await context.SaveChangesAsync();
        }

        var matches = pledges.Where(pledge => byCategory
            ? pledge.PledgeCategoryId == category.Id : pledge.PledgerId == pledger.Id);
        var expected = sortBy == PledgeSortBy.Amount
            ? sortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Amount) : matches.OrderBy(pledge => pledge.Amount)
            : sortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Year) : matches.OrderBy(pledge => pledge.Year);
        var expectedIds = expected.ThenByDescending(pledge => new SqlGuid(pledge.Id.Value))
            .Skip(2).Take(2).Select(pledge => pledge.Id);
        await using var readContext = CreateContext();
        var repository = new SqlServerPledgeRepository(readContext);
        var query = new PledgeQuery(2, 2, sortBy, sortOrder);
        var result = byCategory
            ? await repository.GetByCategoryAsync(category.Id, query)
            : await repository.GetByPledgerAsync(pledger.Id, query);
        Assert.Equal(expectedIds, result.Items.Select(pledge => pledge.Id));
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Default_and_out_of_range_queries_preserve_totals_and_page_sizes()
    {
        var (pledger, category) = await SeedParentsAsync();
        var pledges = Enumerable.Range(0, 21)
            .Select(index => Pledge.Create(2020 + index, index + 1m, pledger.Id, category.Id)).ToList();
        await using (var context = CreateContext())
        {
            context.Pledges.AddRange(pledges);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new SqlServerPledgeRepository(readContext);
        var byPledger = await repository.GetByPledgerAsync(pledger.Id);
        var byCategory = await repository.GetByCategoryAsync(category.Id);
        Assert.Equal(20, byPledger.Items.Count);
        Assert.Equal(21, byPledger.TotalCount);
        Assert.Equal(2, byPledger.TotalPages);
        Assert.Equal(2040, byPledger.Items[0].Year);
        Assert.Equal(byPledger.Items.Select(pledge => pledge.Id), byCategory.Items.Select(pledge => pledge.Id));
        var beyondPledger = await repository.GetByPledgerAsync(pledger.Id, new PledgeQuery(int.MaxValue, 100));
        var beyondCategory = await repository.GetByCategoryAsync(category.Id, new PledgeQuery(int.MaxValue, 100));
        Assert.Empty(beyondPledger.Items);
        Assert.Empty(beyondCategory.Items);
        Assert.Equal(21, beyondPledger.TotalCount);
        Assert.Equal(21, beyondCategory.TotalCount);
    }

    [Fact]
    public async Task Empty_queries_and_unknown_id_reads_preserve_existing_outcomes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeRepository(context);
        Assert.Null(await repository.GetByIdAsync(PledgeId.New()));
        var byPledger = await repository.GetByPledgerAsync(PledgerId.New());
        var byCategory = await repository.GetByCategoryAsync(PledgeCategoryId.New());
        Assert.Empty(byPledger.Items);
        Assert.Empty(byCategory.Items);
        Assert.Equal(0, byPledger.TotalCount);
        Assert.Equal(0, byCategory.TotalCount);
    }

    [Fact]
    public async Task Canceled_calls_do_not_read_or_mutate_the_context()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeRepository(context);
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<CancellationToken, Task>[] operations =
        [
            token => repository.AddAsync(pledge, token),
            token => repository.UpdateAsync(pledge, token),
            token => repository.GetByIdAsync(pledge.Id, token),
            token => repository.GetByPledgerAsync(pledge.PledgerId, cancellationToken: token),
            token => repository.GetByCategoryAsync(pledge.PledgeCategoryId, cancellationToken: token),
            token => repository.RemoveAsync(pledge.Id, token)
        ];
        foreach (var operation in operations)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(cancellation.Token));
        }

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await context.Pledges.ToListAsync());
    }

    [Fact]
    public async Task Referenced_deletes_fail_without_deleting_related_records()
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(
                () => repository.RemoveAsync(pledge.Id));
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await AssertRelatedRecordsExistAsync(pledge, payment);
        await RemoveRelatedRecordsAsync(pledge, payment);
    }

    [Theory]
    [InlineData("Pledger", false)]
    [InlineData("Pledger", true)]
    [InlineData("Category", false)]
    [InlineData("Category", true)]
    public async Task Invalid_reference_writes_fail_without_corrupting_saved_records(
        string missingReference, bool update)
    {
        var (pledge, payment) = await SeedPaymentAsync();
        var originalPledgerId = pledge.PledgerId;
        var originalCategoryId = pledge.PledgeCategoryId;
        await using (var context = CreateContext())
        {
            var invalid = update ? pledge : Pledge.Create(2026, 50m, originalPledgerId, originalCategoryId);
            invalid.Update(2026, 50m,
                missingReference == "Pledger" ? PledgerId.New() : originalPledgerId,
                missingReference == "Category" ? PledgeCategoryId.New() : originalCategoryId);
            var repository = new SqlServerPledgeRepository(context);
            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(
                () => update ? repository.UpdateAsync(invalid) : repository.AddAsync(invalid));
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        var storedPayment = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        Assert.NotNull(stored);
        Assert.NotNull(storedPayment);
        Assert.Equal(originalPledgerId, stored.PledgerId);
        Assert.Equal(originalCategoryId, stored.PledgeCategoryId);
        Assert.Equal(100m, stored.Amount);
        Assert.Equal(pledge.Id, storedPayment.PledgeId);
        Assert.Equal(10m, storedPayment.Amount);
        Assert.Equal(1, await readContext.Pledges.CountAsync());
        Assert.Equal(1, await readContext.PledgePayments.CountAsync());
    }

    [Fact]
    public async Task Tracked_mutations_and_relationship_changes_persist_without_duplicate_tracking()
    {
        var (pledger, category) = await SeedParentsAsync();
        var (otherPledger, otherCategory) = await SeedParentsAsync();
        var pledge = Pledge.Create(2025, 50m, pledger.Id, category.Id);
        await using (var context = CreateContext())
        {
            context.Pledges.Add(pledge);
            await context.SaveChangesAsync();
            pledge.Update(2026, 100m, otherPledger.Id, otherCategory.Id);
            await new SqlServerPledgeRepository(context).UpdateAsync(pledge);
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        Assert.NotNull(stored);
        Assert.Equal(otherPledger.Id, stored.PledgerId);
        Assert.Equal(otherCategory.Id, stored.PledgeCategoryId);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Null_aggregate_mutations_fail_before_tracking_changes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeRepository(context);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Canceled_mutations_preserve_existing_records()
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            pledge.Update(2027, 200m, pledge.PledgerId, pledge.PledgeCategoryId);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.UpdateAsync(pledge, cancellation.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RemoveAsync(pledge.Id, cancellation.Token));
            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        Assert.NotNull(stored);
        Assert.Equal(2026, stored.Year);
        Assert.Equal(100m, stored.Amount);
        await AssertRelatedRecordsExistAsync(pledge, payment);
    }
}