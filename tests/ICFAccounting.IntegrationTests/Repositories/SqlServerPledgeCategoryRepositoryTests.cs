using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace ICFAccounting.IntegrationTests.Repositories;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlServerPledgeCategoryRepositoryTests(SqlServerModelFixture fixture) : SqlServerRepositoryTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Category_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var category = PledgeCategory.Create("Before");
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(category);
            }
            else
            {
                await repository.AddAsync(category);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            var stored = await repository.GetByIdAsync(category.Id);
            Assert.NotNull(stored);
            stored.Update("After", false);
            await repository.AddAsync(stored);
            stored.Update("Final", false);
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            var stored = Assert.Single(await repository.GetAllAsync());
            Assert.Equal(category.Id, stored.Id);
            Assert.Equal("Final", stored.CategoryName);
            Assert.False(stored.Active);
            Assert.Equal(category.CreatedAt, stored.CreatedAt);
            await repository.RemoveAsync(category.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = new SqlServerPledgeCategoryRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(category.Id));
        Assert.Empty(await readRepository.GetAllAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(category.Id));
    }

    [Fact]
    public async Task Empty_queries_and_unknown_id_reads_preserve_existing_outcomes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeCategoryRepository(context);
        Assert.Null(await repository.GetByIdAsync(PledgeCategoryId.New()));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task Canceled_calls_do_not_read_or_mutate_the_context()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeCategoryRepository(context);
        var category = PledgeCategory.Create("Canceled");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<CancellationToken, Task>[] operations =
        [
            token => repository.AddAsync(category, token),
            token => repository.UpdateAsync(category, token),
            token => repository.GetByIdAsync(category.Id, token),
            token => repository.GetAllAsync(token),
            token => repository.RemoveAsync(category.Id, token)
        ];
        foreach (var operation in operations)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(cancellation.Token));
        }

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await context.PledgeCategories.ToListAsync());
    }

    [Fact]
    public async Task Referenced_deletes_fail_without_deleting_related_records()
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(
                () => repository.RemoveAsync(pledge.PledgeCategoryId));
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await AssertRelatedRecordsExistAsync(pledge, payment);
        await RemoveRelatedRecordsAsync(pledge, payment);
    }

    [Fact]
    public async Task Tracked_mutations_persist_without_duplicate_tracking()
    {
        var category = PledgeCategory.Create("Tracked category");
        await using (var context = CreateContext())
        {
            context.PledgeCategories.Add(category);
            await context.SaveChangesAsync();
            category.Update("Updated tracked category", false);
            await new SqlServerPledgeCategoryRepository(context).AddAsync(category);
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgeCategoryRepository(readContext).GetByIdAsync(category.Id);
        Assert.NotNull(stored);
        Assert.Equal("Updated tracked category", stored.CategoryName);
        Assert.False(stored.Active);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Null_aggregate_mutations_fail_before_tracking_changes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeCategoryRepository(context);
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateAsync(null!));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Materialized_lists_remain_unchanged_after_later_writes()
    {
        await using var context = CreateContext();
        var repository = new SqlServerPledgeCategoryRepository(context);
        var original = PledgeCategory.Create("Original category");
        await repository.AddAsync(original);
        var snapshot = await repository.GetAllAsync();
        var remaining = PledgeCategory.Create("Remaining category");
        await repository.AddAsync(remaining);
        await repository.RemoveAsync(original.Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.RemoveAsync(PledgeCategoryId.New()));

        Assert.Equal(original.Id, Assert.Single(snapshot).Id);
        Assert.Equal(remaining.Id, Assert.Single(await repository.GetAllAsync()).Id);
    }

    [Fact]
    public async Task Canceled_mutations_preserve_existing_records()
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            var category = await repository.GetByIdAsync(pledge.PledgeCategoryId);
            Assert.NotNull(category);
            category.Update("Canceled category", false);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.UpdateAsync(category, cancellation.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RemoveAsync(category.Id, cancellation.Token));
            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var readContext = CreateContext();
        var stored = await new SqlServerPledgeCategoryRepository(readContext).GetByIdAsync(pledge.PledgeCategoryId);
        Assert.NotNull(stored);
        Assert.Equal("Test category", stored.CategoryName);
        Assert.True(stored.Active);
        await AssertRelatedRecordsExistAsync(pledge, payment);
    }
}