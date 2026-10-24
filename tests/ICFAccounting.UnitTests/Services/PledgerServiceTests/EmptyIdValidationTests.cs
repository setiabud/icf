using FakeItEasy;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class EmptyIdValidationTests
{
    public static TheoryData<string> Operations => ["GetById", "Update", "Remove"];

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Empty_id_throws_specific_message_and_parameter_without_calling_repository(string operation)
    {
        var fixture = new PledgerServiceTestFixture();
        Func<Task> act = operation switch
        {
            "GetById" => () => fixture.Service.GetByIdAsync(default),
            "Update" => () => fixture.Service.UpdateAsync(default, "Mission Fund", true),
            "Remove" => () => fixture.Service.RemoveAsync(default),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(act);

        Assert.Equal("id", exception.ParamName);
        Assert.Equal("Pledger ID cannot be empty. (Parameter 'id')", exception.Message);
        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }
}
