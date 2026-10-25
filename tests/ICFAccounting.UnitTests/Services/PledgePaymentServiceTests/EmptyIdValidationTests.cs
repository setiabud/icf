using FakeItEasy;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class EmptyIdValidationTests
{
    public static TheoryData<string> Operations => ["GetById", "GetByPledge", "Update", "Remove"];

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Empty_id_throws_without_calling_repository(string operation)
    {
        var fixture = new PledgePaymentServiceTestFixture();
        Func<Task> act = operation switch
        {
            "GetById" => () => fixture.Service.GetByIdAsync(default),
            "GetByPledge" => () => fixture.Service.GetByPledgeAsync(default),
            "Update" => () => fixture.Service.UpdateAsync(
                default, 10m, PledgePaymentServiceTestFixture.StartTime.UtcDateTime, null, true),
            "Remove" => () => fixture.Service.RemoveAsync(default),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await Assert.ThrowsAsync<ArgumentException>(act);

        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }
}