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
        var logger = A.Fake<ILogger<ICFAccounting.Application.Services.PledgerService>>();

        Assert.Throws<ArgumentNullException>(() => new ICFAccounting.Application.Services.PledgerService(null!, logger));
    }

    [Fact]
    public void Constructor_with_null_logger_throws()
    {
        var repository = A.Fake<IPledgerRepository>();

        Assert.Throws<ArgumentNullException>(() => new ICFAccounting.Application.Services.PledgerService(repository, null!));
    }
}
