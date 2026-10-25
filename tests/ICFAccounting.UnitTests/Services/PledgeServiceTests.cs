using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services;

public sealed class PledgeServiceTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_forwards_pledge_and_cancellation_token()
    {
        var (service, repository, _) = CreateService();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        using var cancellationTokenSource = new CancellationTokenSource();

        await service.AddAsync(pledge, cancellationTokenSource.Token);

        A.CallTo(() => repository.AddAsync(pledge, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetByIdAsync_returns_pledge_from_repository()
    {
        var (service, repository, _) = CreateService();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        A.CallTo(() => repository.GetByIdAsync(pledge.Id, A<CancellationToken>._)).Returns(pledge);

        var result = await service.GetByIdAsync(pledge.Id);

        Assert.Same(pledge, result);
    }

    [Fact]
    public async Task GetByIdAsync_with_empty_id_throws_without_calling_repository()
    {
        var (service, repository, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByIdAsync(default));

        A.CallTo(() => repository.GetByIdAsync(A<PledgeId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task GetByPledgerAsync_uses_default_query_and_returns_repository_page()
    {
        var (service, repository, _) = CreateService();
        var pledgerId = PledgerId.New();
        var repositoryResult = PagedPledgesResult.Create([], 1, 20, 0);
        A.CallTo(() => repository.GetByPledgerAsync(
                pledgerId,
                A<PledgeQuery>.That.Matches(query => query.Page == 1 && query.PageSize == 20
                    && query.SortBy == PledgeSortBy.Year && query.SortOrder == SortOrder.Descending),
                A<CancellationToken>._))
            .Returns(repositoryResult);

        var result = await service.GetByPledgerAsync(pledgerId);

        Assert.Same(repositoryResult, result);
    }

    [Fact]
    public async Task GetByPledgerAsync_forwards_pagination_options()
    {
        var (service, repository, _) = CreateService();
        var pledgerId = PledgerId.New();
        var query = new PledgeQuery(page: 3, pageSize: 7);
        var repositoryResult = PagedPledgesResult.Create([], 3, 7, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => repository.GetByPledgerAsync(pledgerId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await service.GetByPledgerAsync(pledgerId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => repository.GetByPledgerAsync(pledgerId, query, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task GetByPledgerAsync_forwards_sorting_options(SortOrder sortOrder)
    {
        var (service, repository, _) = CreateService();
        var pledgerId = PledgerId.New();
        var query = new PledgeQuery(sortBy: PledgeSortBy.Year, sortOrder: sortOrder);
        var repositoryResult = PagedPledgesResult.Create([], 1, 20, 0);
        A.CallTo(() => repository.GetByPledgerAsync(pledgerId, query, CancellationToken.None))
            .Returns(repositoryResult);

        var result = await service.GetByPledgerAsync(pledgerId, query);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => repository.GetByPledgerAsync(pledgerId, query, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetByCategoryAsync_forwards_query_and_cancellation_token()
    {
        var (service, repository, _) = CreateService();
        var categoryId = PledgeCategoryId.New();
        var query = new PledgeQuery(page: 2, pageSize: 5, sortOrder: SortOrder.Ascending);
        var repositoryResult = PagedPledgesResult.Create([], 2, 5, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => repository.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await service.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
    }

    [Fact]
    public async Task GetByCategoryAsync_forwards_pagination_options()
    {
        var (service, repository, _) = CreateService();
        var categoryId = PledgeCategoryId.New();
        var query = new PledgeQuery(page: 4, pageSize: 6);
        var repositoryResult = PagedPledgesResult.Create([], 4, 6, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => repository.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await service.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => repository.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task GetByCategoryAsync_forwards_sorting_options(SortOrder sortOrder)
    {
        var (service, repository, _) = CreateService();
        var categoryId = PledgeCategoryId.New();
        var query = new PledgeQuery(sortBy: PledgeSortBy.Year, sortOrder: sortOrder);
        var repositoryResult = PagedPledgesResult.Create([], 1, 20, 0);
        A.CallTo(() => repository.GetByCategoryAsync(categoryId, query, CancellationToken.None))
            .Returns(repositoryResult);

        var result = await service.GetByCategoryAsync(categoryId, query);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => repository.GetByCategoryAsync(categoryId, query, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_updates_pledge_with_injected_time_provider()
    {
        var (service, repository, clock) = CreateService();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New(), "Original", clock);
        var originalCreatedAt = pledge.CreatedAt;
        var updatedPledgerId = PledgerId.New();
        var updatedCategoryId = PledgeCategoryId.New();
        clock.Advance(TimeSpan.FromMinutes(5));
        A.CallTo(() => repository.GetByIdAsync(pledge.Id, A<CancellationToken>._)).Returns(pledge);

        var result = await service.UpdateAsync(
            pledge.Id, 2027, 125m, updatedPledgerId, updatedCategoryId, "Updated");

        Assert.Same(pledge, result);
        Assert.Equal(2027, pledge.Year);
        Assert.Equal(125m, pledge.Amount);
        Assert.Equal(updatedPledgerId, pledge.PledgerId);
        Assert.Equal(updatedCategoryId, pledge.PledgeCategoryId);
        Assert.Equal("Updated", pledge.Comment);
        Assert.Equal(originalCreatedAt, pledge.CreatedAt);
        Assert.Equal(StartTime.AddMinutes(5).UtcDateTime, pledge.UpdatedAt);
        A.CallTo(() => repository.UpdateAsync(pledge, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_when_pledge_does_not_exist_returns_null_without_updating()
    {
        var (service, repository, _) = CreateService();
        var id = PledgeId.New();
        A.CallTo(() => repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((Pledge?)null);

        var result = await service.UpdateAsync(id, 2026, 100m, PledgerId.New(), PledgeCategoryId.New(), null);

        Assert.Null(result);
        A.CallTo(() => repository.UpdateAsync(A<Pledge>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task RemoveAsync_forwards_id_and_cancellation_token()
    {
        var (service, repository, _) = CreateService();
        var id = PledgeId.New();
        using var cancellationTokenSource = new CancellationTokenSource();

        await service.RemoveAsync(id, cancellationTokenSource.Token);

        A.CallTo(() => repository.RemoveAsync(id, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    private static (PledgeService Service, IPledgeRepository Repository, FakeTimeProvider Clock) CreateService()
    {
        var repository = A.Fake<IPledgeRepository>();
        var logger = A.Fake<ILogger<PledgeService>>();
        var clock = new FakeTimeProvider(StartTime);
        var service = new PledgeService(repository, logger, clock);
        return (service, repository, clock);
    }
}