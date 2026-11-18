namespace ICFAccounting.IntegrationTests.Data;

/// <summary>
/// Shares one PostgreSQL container across persistence tests without running them concurrently.
/// </summary>
[CollectionDefinition(nameof(PostgresTestCollection))]
public sealed class PostgresTestCollection : ICollectionFixture<PostgresModelFixture>
{
}