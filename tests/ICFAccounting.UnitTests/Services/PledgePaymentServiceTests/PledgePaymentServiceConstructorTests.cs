using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class PledgePaymentServiceConstructorTests
{
    [Fact]
    public void Constructor_with_null_repository_throws()
    {
        var logger = A.Fake<ILogger<PledgePaymentService>>();

        Assert.Throws<ArgumentNullException>(
            () => new PledgePaymentService(null!, logger, TimeProvider.System));
    }

    [Fact]
    public void Constructor_with_null_logger_throws()
    {
        var repository = A.Fake<IPledgePaymentRepository>();

        Assert.Throws<ArgumentNullException>(
            () => new PledgePaymentService(repository, null!, TimeProvider.System));
    }

    [Fact]
    public void Constructor_with_null_time_provider_throws()
    {
        var repository = A.Fake<IPledgePaymentRepository>();
        var logger = A.Fake<ILogger<PledgePaymentService>>();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new PledgePaymentService(repository, logger, null!));

        Assert.Equal("timeProvider", exception.ParamName);
    }
}