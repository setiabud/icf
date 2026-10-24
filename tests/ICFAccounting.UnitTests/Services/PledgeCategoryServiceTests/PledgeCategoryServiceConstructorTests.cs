using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class PledgeCategoryServiceConstructorTests
{
    [Fact]
    public void Constructor_with_null_repository_throws()
    {
        var logger = A.Fake<ILogger<PledgeCategoryService>>();

        Assert.Throws<ArgumentNullException>(() => new PledgeCategoryService(null!, logger, TimeProvider.System));
    }

    [Fact]
    public void Constructor_with_null_logger_throws()
    {
        var repository = A.Fake<IPledgeCategoryRepository>();

        Assert.Throws<ArgumentNullException>(() => new PledgeCategoryService(repository, null!, TimeProvider.System));
    }

    [Fact]
    public void Constructor_with_null_time_provider_throws()
    {
        var repository = A.Fake<IPledgeCategoryRepository>();
        var logger = A.Fake<ILogger<PledgeCategoryService>>();

        var exception = Assert.Throws<ArgumentNullException>(() => new PledgeCategoryService(repository, logger, null!));
        Assert.Equal("timeProvider", exception.ParamName);
    }
}
