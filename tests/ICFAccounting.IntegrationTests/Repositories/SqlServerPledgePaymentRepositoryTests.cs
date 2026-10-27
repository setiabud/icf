using System.Data.SqlTypes;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace ICFAccounting.IntegrationTests.Repositories;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlServerPledgePaymentRepositoryTests(SqlServerModelFixture fixture) : SqlServerRepositoryTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Payment_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var pledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2025, 1, 1));
        await using (var context = CreateContext())
        {
            var repository = CreatePaymentRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(payment);
            }
            else
            {
                await repository.AddAsync(payment);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = CreatePaymentRepository(context);
            var stored = await repository.GetByIdAsync(payment.Id);
            Assert.NotNull(stored);
            stored.Update(pledge.Id, 20m, new DateTime(2026, 10, 26), "After", true);
            await repository.AddAsync(stored);
            stored.Update(pledge.Id, decimal.MaxValue, new DateTime(2026, 10, 27), "Final", false);
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = CreatePaymentRepository(context);
            var stored = await repository.GetByIdAsync(payment.Id);
            Assert.NotNull(stored);
            Assert.Equal(decimal.MaxValue, stored.Amount);
            Assert.Equal(new DateTime(2026, 10, 27), stored.PaymentDate);
            Assert.Equal("Final", stored.Comment);
            Assert.False(stored.Active);
            Assert.Equal(payment.CreatedAt, stored.CreatedAt);
            Assert.Contains((await repository.GetByPledgeAsync(pledge.Id)).Items, entity => entity.Id == payment.Id);
            await repository.RemoveAsync(payment.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = CreatePaymentRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(payment.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(payment.Id));
    }

    [Theory]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Descending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Descending)]
    public async Task Payment_queries_filter_sort_and_page_in_sql(
        PledgePaymentSortBy sortBy, SortOrder sortOrder)
    {
        var pledge = await SeedPledgeAsync();
        var otherPledge = await SeedPledgeAsync();
        var amounts = new[] { 10m, decimal.MaxValue, 2m, 10m, 0.01m };
        var payments = amounts.Select((amount, index) => PledgePayment.Create(
            pledge.Id, amount, new DateTime(2026, 10, 26).AddDays(index % 2))).ToList();
        payments[0].Update(pledge.Id, payments[0].Amount, payments[0].PaymentDate, null, false);
        await using (var context = CreateContext())
        {
            context.PledgePayments.AddRange(payments);
            context.PledgePayments.Add(PledgePayment.Create(otherPledge.Id, 999m, new DateTime(2026, 1, 1)));
            await context.SaveChangesAsync();
        }

        var expected = sortBy == PledgePaymentSortBy.Amount
            ? sortOrder == SortOrder.Descending
                ? payments.OrderByDescending(payment => payment.Amount) : payments.OrderBy(payment => payment.Amount)
            : sortOrder == SortOrder.Descending
                ? payments.OrderByDescending(payment => payment.PaymentDate) : payments.OrderBy(payment => payment.PaymentDate);
        var expectedIds = expected.ThenByDescending(payment => new SqlGuid(payment.Id.Value))
            .Skip(2).Take(2).Select(payment => payment.Id);
        await using var readContext = CreateContext();
        var repository = CreatePaymentRepository(readContext);
        var result = await repository.GetByPledgeAsync(pledge.Id, new PledgePaymentQuery(2, 2, sortBy, sortOrder));
        Assert.Equal(expectedIds, result.Items.Select(payment => payment.Id));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Default_and_out_of_range_queries_preserve_totals_and_page_sizes()
    {
        var pledge = await SeedPledgeAsync();
        var payments = Enumerable.Range(0, 21)
            .Select(index => PledgePayment.Create(pledge.Id, index + 1m, new DateTime(2026, 1, 1).AddDays(index)))
            .ToList();
        await using (var context = CreateContext())
        {
            context.PledgePayments.AddRange(payments);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = CreatePaymentRepository(readContext);
        var result = await repository.GetByPledgeAsync(pledge.Id);
        Assert.Equal(20, result.Items.Count);
        Assert.Equal(21, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(new DateTime(2026, 1, 21), result.Items[0].PaymentDate);
        var beyondEnd = await repository.GetByPledgeAsync(pledge.Id, new PledgePaymentQuery(int.MaxValue, 100));
        Assert.Empty(beyondEnd.Items);
        Assert.Equal(21, beyondEnd.TotalCount);
    }

    [Fact]
    public async Task Empty_queries_and_unknown_id_reads_preserve_existing_outcomes()
    {
        await using var context = CreateContext();
        var repository = CreatePaymentRepository(context);
        Assert.Null(await repository.GetByIdAsync(PledgePaymentId.New()));
        var result = await repository.GetByPledgeAsync(PledgeId.New());
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Canceled_calls_do_not_read_or_mutate_the_context()
    {
        await using var context = CreateContext();
        var repository = CreatePaymentRepository(context);
        var payment = PledgePayment.Create(PledgeId.New(), 10m, new DateTime(2026, 10, 27));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<CancellationToken, Task>[] operations =
        [
            token => repository.AddAsync(payment, token),
            token => repository.UpdateAsync(payment, token),
            token => repository.GetByIdAsync(payment.Id, token),
            token => repository.GetByPledgeAsync(payment.PledgeId, cancellationToken: token),
            token => repository.RemoveAsync(payment.Id, token)
        ];
        foreach (var operation in operations)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(cancellation.Token));
        }

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await context.PledgePayments.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_reference_writes_fail_without_corrupting_saved_records(bool update)
    {
        var (pledge, payment) = await SeedPaymentAsync();
        await using (var context = CreateContext())
        {
            var invalid = update ? payment
                : PledgePayment.Create(pledge.Id, 20m, new DateTime(2026, 10, 27));
            invalid.Update(PledgeId.New(), 20m, invalid.PaymentDate, null, true);
            var repository = CreatePaymentRepository(context);
            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(
                () => update ? repository.UpdateAsync(invalid) : repository.AddAsync(invalid));
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await using var readContext = CreateContext();
        var stored = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        var storedPledge = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        Assert.NotNull(stored);
        Assert.NotNull(storedPledge);
        Assert.Equal(pledge.Id, stored.PledgeId);
        Assert.Equal(10m, stored.Amount);
        Assert.Equal(pledge.PledgerId, storedPledge.PledgerId);
        Assert.Equal(pledge.PledgeCategoryId, storedPledge.PledgeCategoryId);
        Assert.Equal(100m, storedPledge.Amount);
        Assert.Equal(1, await readContext.Pledges.CountAsync());
        Assert.Equal(1, await readContext.PledgePayments.CountAsync());
    }

    [Fact]
    public async Task Tracked_mutations_and_relationship_changes_persist_without_duplicate_tracking()
    {
        var pledge = await SeedPledgeAsync();
        var otherPledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 1, 1));
        await using (var context = CreateContext())
        {
            context.PledgePayments.Add(payment);
            await context.SaveChangesAsync();
            payment.Update(otherPledge.Id, 20m, new DateTime(2026, 10, 27), null, false);
            await CreatePaymentRepository(context).AddAsync(payment);
        }

        await using var readContext = CreateContext();
        var stored = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        Assert.NotNull(stored);
        Assert.Equal(otherPledge.Id, stored.PledgeId);
        Assert.False(stored.Active);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Null_aggregate_mutations_fail_before_tracking_changes()
    {
        await using var context = CreateContext();
        var repository = CreatePaymentRepository(context);
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
            var repository = CreatePaymentRepository(context);
            payment.Update(pledge.Id, 20m, payment.PaymentDate, null, false);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.UpdateAsync(payment, cancellation.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RemoveAsync(payment.Id, cancellation.Token));
            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var readContext = CreateContext();
        var stored = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        Assert.NotNull(stored);
        Assert.Equal(10m, stored.Amount);
        Assert.True(stored.Active);
        await AssertRelatedRecordsExistAsync(pledge, payment);
    }
}