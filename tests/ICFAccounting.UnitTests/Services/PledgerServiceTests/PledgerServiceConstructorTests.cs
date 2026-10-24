using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class PledgerServiceConstructorTests
{
    [Fact]
    public void Constructor_with_null_repository_throws()
    {
        var logger = A.Fake<ILogger<PledgerService>>();

        Assert.Throws<ArgumentNullException>(() => new PledgerService(null!, logger, TimeProvider.System));
    }

    [Fact]
    public void Constructor_with_null_logger_throws()
    {
        var repository = A.Fake<IPledgerRepository>();

        Assert.Throws<ArgumentNullException>(() => new PledgerService(repository, null!, TimeProvider.System));
    }

    [Fact]
    public void Constructor_with_null_time_provider_throws()
    {
        var repository = A.Fake<IPledgerRepository>();
        var logger = A.Fake<ILogger<PledgerService>>();

        var exception = Assert.Throws<ArgumentNullException>(() => new PledgerService(repository, logger, null!));
        Assert.Equal("timeProvider", exception.ParamName);
    }
}
