using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgeSummaryServiceTests;

public sealed class CreateAsyncTests
{
    [Fact]
    public async Task CreateAsync_in_january_includes_current_and_prior_year()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var pledgerRepository = A.Fake<IPledgerRepository>();
        var pledgeRepository = A.Fake<IPledgeRepository>();
        var paymentRepository = A.Fake<IPledgePaymentRepository>();
        var categoryRepository = A.Fake<IPledgeCategoryRepository>();
        var pledger = Pledger.Create(
            "Ada Lovelace",
            clock,
            "ada@example.com",
            enableNotification: true
        );
        var category = PledgeCategory.Create("General Fund", clock);
        var currentPledge = Pledge.Create(2026, 1200m, pledger.Id, category.Id, "Annual", clock);
        var priorPledge = Pledge.Create(2025, 1000m, pledger.Id, category.Id, "Annual", clock);
        var currentPayment = PledgePayment.Create(
            currentPledge.Id,
            300m,
            new DateTime(2026, 1, 10),
            "January",
            clock
        );
        var futurePayment = PledgePayment.Create(
            currentPledge.Id,
            900m,
            new DateTime(2026, 2, 10),
            "Future",
            clock
        );
        var priorPayment = PledgePayment.Create(
            priorPledge.Id,
            1000m,
            new DateTime(2025, 12, 31),
            "Paid",
            clock
        );

        A.CallTo(() => pledgerRepository.GetAllAsync(null, A<CancellationToken>._))
            .Returns(PagedPledgersResult.Create([pledger], 1, 100, 1));
        A.CallTo(() => categoryRepository.GetAllAsync(A<CancellationToken>._))
            .Returns([category]);
        A.CallTo(() => pledgeRepository.GetByPledgerAsync(
                pledger.Id,
                A<PledgeQuery>.That.Matches(query => query.PageSize == 100),
                A<CancellationToken>._
            ))
            .ReturnsLazily(call =>
            {
                var query = call.GetArgument<PledgeQuery>(1)!;
                var pledges = query.Page == 1 ? new Pledge[] { currentPledge, priorPledge } : Array.Empty<Pledge>();
                return Task.FromResult(PagedPledgesResult.Create(pledges, query.Page, query.PageSize, 2));
            });
        A.CallTo(() => paymentRepository.GetByPledgeAsync(
                A<PledgeId>._,
                A<PledgePaymentQuery>.That.Matches(query => query.PageSize == 100),
                A<CancellationToken>._
            ))
            .ReturnsLazily(call =>
            {
                var pledgeId = call.GetArgument<PledgeId>(0);
                var query = call.GetArgument<PledgePaymentQuery>(1)!;
                var payments = pledgeId == currentPledge.Id
                    ? new[] { currentPayment, futurePayment }
                    : new[] { priorPayment };
                return Task.FromResult(
                    PagedPledgePaymentsResult.Create(payments, query.Page, query.PageSize, payments.Length)
                );
            });

        var service = new PledgeSummaryService(
            pledgerRepository,
            pledgeRepository,
            paymentRepository,
            categoryRepository,
            clock
        );

        var summaries = await service.CreateAsync();

        Assert.Single(summaries);
        Assert.Contains("2026", summaries[0].Html);
        Assert.Contains("2025", summaries[0].Html);
        Assert.Contains("300", summaries[0].Html);
        Assert.DoesNotContain(">900<", summaries[0].Html);
        Assert.Contains("1,000", summaries[0].Html);
    }

    [Fact]
    public async Task CreateAsync_excludes_pledgers_without_notifications()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var pledgerRepository = A.Fake<IPledgerRepository>();
        var pledger = Pledger.Create("Inactive notifications", clock, "person@example.com");
        var pledgeRepository = A.Fake<IPledgeRepository>();
        var paymentRepository = A.Fake<IPledgePaymentRepository>();
        var categoryRepository = A.Fake<IPledgeCategoryRepository>();
        A.CallTo(() => pledgerRepository.GetAllAsync(null, A<CancellationToken>._))
            .Returns(PagedPledgersResult.Create([pledger], 1, 100, 1));
        A.CallTo(() => categoryRepository.GetAllAsync(A<CancellationToken>._))
            .Returns([]);
        var service = new PledgeSummaryService(
            pledgerRepository,
            pledgeRepository,
            paymentRepository,
            categoryRepository,
            clock
        );

        var summaries = await service.CreateAsync();

        Assert.Empty(summaries);
    }
}
