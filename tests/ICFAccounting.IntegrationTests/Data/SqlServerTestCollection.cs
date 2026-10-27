namespace ICFAccounting.IntegrationTests.Data;

/// <summary>
/// Shares one SQL Server container across persistence tests without running them concurrently.
/// </summary>
[CollectionDefinition(nameof(SqlServerTestCollection))]
public sealed class SqlServerTestCollection : ICollectionFixture<SqlServerModelFixture>
{
}