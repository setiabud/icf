using System.Data.SqlTypes;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace ICFAccounting.IntegrationTests.Repositories;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlServerPledgerRepositoryTests(SqlServerModelFixture fixture) : SqlServerRepositoryTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pledger_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var pledger = Pledger.Create("Before");
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(pledger);
            }
            else
            {
                await repository.AddAsync(pledger);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            var stored = await repository.GetByIdAsync(pledger.Id);
            Assert.NotNull(stored);
            Assert.Equal(pledger.CreatedAt, stored.CreatedAt);
            stored.Update("After", false);
            await repository.AddAsync(stored);
            stored.Update("Final", false);
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            var stored = await repository.GetByIdAsync(pledger.Id);
            Assert.NotNull(stored);
            Assert.Equal("Final", stored.PledgerName);
            Assert.False(stored.Active);
            Assert.Equal(pledger.CreatedAt, stored.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
            await repository.RemoveAsync(pledger.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = new SqlServerPledgerRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(pledger.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(pledger.Id));
    }

    [Fact]
    public async Task Pledger_queries_preserve_null_query_and_paging_contracts()
    {
        var pledgers = Enumerable.Range(1, 21).Select(index => Pledger.Create($"Pledger {index:D2}")).ToList();
        pledgers[0].Update(pledgers[0].PledgerName, false);
        await using (var context = CreateContext())
        {
            context.Pledgers.AddRange(pledgers);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new SqlServerPledgerRepository(readContext);
        var unpaged = await repository.GetAllAsync();
        Assert.Equal(21, unpaged.Items.Count);
        Assert.Equal(21, unpaged.TotalCount);
        Assert.Equal(21, unpaged.PageSize);
        Assert.Equal(1, unpaged.Page);
        Assert.Contains(unpaged.Items, pledger => !pledger.Active);

        var firstPage = await repository.GetAllAsync(new PledgerQuery());
        Assert.Equal(20, firstPage.Items.Count);
        Assert.Equal(21, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        var descending = await repository.GetAllAsync(new PledgerQuery(2, 2, sortOrder: SortOrder.Descending));
        Assert.Equal(["Pledger 19", "Pledger 18"], descending.Items.Select(pledger => pledger.PledgerName));
        var beyondEnd = await repository.GetAllAsync(new PledgerQuery(int.MaxValue, 100));
        Assert.Empty(beyondEnd.Items);
        Assert.Equal(21, beyondEnd.TotalCount);
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task Pledger_queries_use_case_insensitive_names_and_sql_guid_ties(SortOrder sortOrder)
    {
        var pledgers = new[] { "alpha", "ALPHA", "beta", "Beta" }
            .Select(name => Pledger.Create(name)).ToList();
        await using (var context = CreateContext())
        {
            context.Pledgers.AddRange(pledgers);
            await context.SaveChangesAsync();
        }

        var expected = sortOrder == SortOrder.Descending
            ? pledgers.OrderByDescending(pledger => pledger.PledgerName, StringComparer.OrdinalIgnoreCase)
            : pledgers.OrderBy(pledger => pledger.PledgerName, StringComparer.OrdinalIgnoreCase);
        var expectedIds = expected.ThenBy(pledger => new SqlGuid(pledger.Id.Value)).Select(pledger => pledger.Id);
        await using var readContext = CreateContext();
        var repository = new SqlServerPledgerRepository(readContext);
        var result = await repository.GetAllAsync(new PledgerQuery(sortOrder: sortOrder));
        Assert.Equal(expectedIds, result.Items.Select(pledger => pledger.Id));
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Empty_queries_and_unknown_id_reads_preserve_existing_outcomes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgerRepository(context);
        Assert.Null(await repository.GetByIdAsync(PledgerId.New()));
        var unpaged = await repository.GetAllAsync();
        Assert.Empty(unpaged.Items);
        Assert.Equal(1, unpaged.PageSize);
        var paged = await repository.GetAllAsync(new PledgerQuery());
        Assert.Empty(paged.Items);
        Assert.Equal(0, paged.TotalCount);
    }

    [Fact]
    public async Task Canceled_calls_do_not_read_or_mutate_the_context()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgerRepository(context);
        var pledger = Pledger.Create("Canceled");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<CancellationToken, Task>[] operations =
        [
            token => repository.AddAsync(pledger, token),
            token => repository.UpdateAsync(pledger, token),
            token => repository.GetByIdAsync(pledger.Id, token),
            token => repository.GetAllAsync(cancellationToken: token),
            token => repository.GetAllAsync(new PledgerQuery(), token),
            token => repository.RemoveAsync(pledger.Id, token)
        ];
        foreach (var operation in operations)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(cancellation.Token));
        }

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await context.Pledgers.ToListAsync());
    }

    [Fact]
    public async Task Referenced_deletes_fail_without_deleting_related_records()
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(
                () => repository.RemoveAsync(pledge.PledgerId));
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await AssertRelatedRecordsExistAsync(pledge, payment);
        await RemoveRelatedRecordsAsync(pledge, payment);
    }

    [Fact]
    public async Task Tracked_mutations_persist_without_duplicate_tracking()
    {
        var pledger = Pledger.Create("Tracked pledger");
        await using (var context = CreateContext())
        {
            context.Pledgers.Add(pledger);
            await context.SaveChangesAsync();
            pledger.Update("Updated tracked pledger", false);
            await new SqlServerPledgerRepository(context).UpdateAsync(pledger);
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgerRepository(readContext).GetByIdAsync(pledger.Id);
        Assert.NotNull(stored);
        Assert.Equal("Updated tracked pledger", stored.PledgerName);
        Assert.False(stored.Active);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Null_aggregate_mutations_fail_before_tracking_changes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgerRepository(context);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Materialized_lists_remain_unchanged_after_later_writes(bool paginated)
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgerRepository(context);
        var original = Pledger.Create("Original pledger");
        await repository.AddAsync(original);
        var snapshot = await repository.GetAllAsync(paginated ? new PledgerQuery() : null);
        var remaining = Pledger.Create("Remaining pledger");
        await repository.AddAsync(remaining);
        await repository.RemoveAsync(original.Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.RemoveAsync(PledgerId.New()));

        Assert.Equal(original.Id, Assert.Single(snapshot.Items).Id);
        Assert.Equal(1, snapshot.TotalCount);
        Assert.Equal(remaining.Id, Assert.Single((await repository.GetAllAsync()).Items).Id);
    }

    [Fact]
    public async Task Canceled_mutations_preserve_existing_records()
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            var pledger = await repository.GetByIdAsync(pledge.PledgerId);
            Assert.NotNull(pledger);
            pledger.Update("Canceled name", false);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.UpdateAsync(pledger, cancellation.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RemoveAsync(pledger.Id, cancellation.Token));
            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgerRepository(readContext).GetByIdAsync(pledge.PledgerId);
        Assert.NotNull(stored);
        Assert.Equal("Test pledger", stored.PledgerName);
        Assert.True(stored.Active);
        await AssertRelatedRecordsExistAsync(pledge, payment);
    }
}