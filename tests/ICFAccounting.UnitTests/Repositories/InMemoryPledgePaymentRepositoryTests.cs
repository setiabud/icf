using FakeItEasy;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.UnitTests.Repositories;

public sealed class InMemoryPledgePaymentRepositoryTests
{
    private static readonly PledgeId PledgeA = PledgeId.New();

    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_payment()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));

        await repository.AddAsync(payment, CancellationToken.None);

        Assert.Same(payment, await repository.GetByIdAsync(payment.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_with_unknown_id_returns_null()
    {
        var repository = CreateRepository();

        Assert.Null(await repository.GetByIdAsync(PledgePaymentId.New(), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_with_null_payment_throws()
    {
        var repository = CreateRepository();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.AddAsync(null!, CancellationToken.None));

        Assert.Equal("pledgePayment", exception.ParamName);
    }

    [Fact]
    public async Task UpdateAsync_replaces_stored_payment()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));
        await repository.AddAsync(payment, CancellationToken.None);

        payment.Update(125m, new DateTime(2026, 1, 15), "Updated", false);
        await repository.UpdateAsync(payment, CancellationToken.None);

        var storedPayment = await repository.GetByIdAsync(payment.Id, CancellationToken.None);
        Assert.Same(payment, storedPayment);
        Assert.Equal(125m, storedPayment!.Amount);
        Assert.Equal(new DateTime(2026, 1, 15), storedPayment.PaymentDate);
        Assert.False(storedPayment.Active);
    }

    [Fact]
    public async Task UpdateAsync_with_unknown_payment_adds_it()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));

        await repository.UpdateAsync(payment, CancellationToken.None);

        Assert.Same(payment, await repository.GetByIdAsync(payment.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_with_null_payment_throws()
    {
        var repository = CreateRepository();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.UpdateAsync(null!, CancellationToken.None));

        Assert.Equal("pledgePayment", exception.ParamName);
    }

    [Fact]
    public async Task RemoveAsync_deletes_payment()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));
        await repository.AddAsync(payment, CancellationToken.None);

        await repository.RemoveAsync(payment.Id, CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(payment.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_with_unknown_id_throws_key_not_found()
    {
        var repository = CreateRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.RemoveAsync(PledgePaymentId.New(), CancellationToken.None));
    }

    [Theory]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Ascending, "150,50,100")]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Descending, "100,50,150")]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Ascending, "50,100,150")]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Descending, "150,100,50")]
    public async Task GetByPledgeAsync_filters_sorts_and_pages(
        PledgePaymentSortBy sortBy, SortOrder sortOrder, string expectedAmounts)
    {
        var repository = CreateRepository();
        await repository.AddAsync(CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 3)));
        await repository.AddAsync(CreatePayment(PledgeA, 150m, new DateTime(2026, 1, 1)));
        await repository.AddAsync(CreatePayment(PledgeA, 50m, new DateTime(2026, 1, 2)));
        await repository.AddAsync(CreatePayment(PledgeId.New(), 1m, new DateTime(2026, 1, 4)));

        var result = await repository.GetByPledgeAsync(
            PledgeA,
            new PledgePaymentQuery(pageSize: 10, sortBy: sortBy, sortOrder: sortOrder),
            CancellationToken.None);

        Assert.Equal(expectedAmounts, string.Join(",", result.Items.Select(payment => payment.Amount)));
        Assert.All(result.Items, payment => Assert.Equal(PledgeA, payment.PledgeId));
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetByPledgeAsync_applies_pagination_and_default_query()
    {
        var repository = CreateRepository();
        await repository.AddAsync(CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 1)));
        await repository.AddAsync(CreatePayment(PledgeA, 200m, new DateTime(2026, 1, 3)));
        await repository.AddAsync(CreatePayment(PledgeA, 300m, new DateTime(2026, 1, 2)));

        var defaultResult = await repository.GetByPledgeAsync(PledgeA);
        var pagedResult = await repository.GetByPledgeAsync(
            PledgeA, new PledgePaymentQuery(page: 2, pageSize: 1, sortOrder: SortOrder.Ascending));

        Assert.Equal(new[] { 200m, 300m, 100m }, defaultResult.Items.Select(payment => payment.Amount));
        Assert.Equal(1, defaultResult.Page);
        Assert.Equal(20, defaultResult.PageSize);
        Assert.Equal(3, defaultResult.TotalCount);
        Assert.Equal(2, pagedResult.Page);
        Assert.Equal(1, pagedResult.PageSize);
        Assert.Equal(3, pagedResult.TotalCount);
        Assert.Equal(3, pagedResult.TotalPages);
        Assert.Equal(300m, Assert.Single(pagedResult.Items).Amount);
    }

    [Fact]
    public async Task GetByPledgeAsync_with_page_beyond_end_returns_empty_items_with_totals()
    {
        var repository = CreateRepository();
        await repository.AddAsync(CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 1)));
        await repository.AddAsync(CreatePayment(PledgeA, 200m, new DateTime(2026, 1, 2)));

        var result = await repository.GetByPledgeAsync(PledgeA, new PledgePaymentQuery(page: 3, pageSize: 2));

        Assert.Empty(result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Theory]
    [InlineData(PledgePaymentSortBy.PaymentDate)]
    [InlineData(PledgePaymentSortBy.Amount)]
    public async Task GetByPledgeAsync_with_equal_sort_values_orders_by_id_descending(PledgePaymentSortBy sortBy)
    {
        var repository = CreateRepository();
        var payments = Enumerable.Range(0, 5)
            .Select(_ => CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 1)))
            .ToList();
        foreach (var payment in payments)
        {
            await repository.AddAsync(payment);
        }

        var firstPage = await repository.GetByPledgeAsync(
            PledgeA, new PledgePaymentQuery(page: 1, pageSize: 3, sortBy: sortBy, sortOrder: SortOrder.Ascending));
        var secondPage = await repository.GetByPledgeAsync(
            PledgeA, new PledgePaymentQuery(page: 2, pageSize: 3, sortBy: sortBy, sortOrder: SortOrder.Ascending));

        var expectedIds = payments.Select(payment => payment.Id.Value).OrderByDescending(id => id);
        Assert.Equal(
            expectedIds,
            firstPage.Items.Concat(secondPage.Items).Select(payment => payment.Id.Value));
    }

    [Fact]
    public async Task AddAsync_with_canceled_token_throws_without_adding_payment()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(payment, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(payment.Id));
    }

    [Fact]
    public async Task UpdateAsync_with_canceled_token_throws_without_adding_payment()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.UpdateAsync(payment, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(payment.Id));
    }

    [Fact]
    public async Task RemoveAsync_with_canceled_token_throws_without_removing_payment()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));
        await repository.AddAsync(payment);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.RemoveAsync(payment.Id, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Same(payment, await repository.GetByIdAsync(payment.Id));
    }

    [Fact]
    public async Task GetByIdAsync_with_canceled_token_throws()
    {
        var repository = CreateRepository();
        var payment = CreatePayment(PledgeA, 100m, new DateTime(2026, 1, 10));
        await repository.AddAsync(payment);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(payment.Id, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task GetByPledgeAsync_with_canceled_token_throws()
    {
        var repository = CreateRepository();
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByPledgeAsync(PledgeA, cancellationToken: tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
    }

    private static InMemoryPledgePaymentRepository CreateRepository()
    {
        return new InMemoryPledgePaymentRepository(A.Fake<ILogger<InMemoryPledgePaymentRepository>>());
    }

    private static PledgePayment CreatePayment(PledgeId pledgeId, decimal amount, DateTime paymentDate)
    {
        return PledgePayment.Create(pledgeId, amount, paymentDate);
    }
}