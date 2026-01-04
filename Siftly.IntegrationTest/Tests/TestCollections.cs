namespace EfCore.Querying.Tests.Integration.Tests;

/// <summary>
/// xUnit Collection Definition for SQL Server tests
/// All tests in this collection share the same SqlServerFixture instance
/// </summary>
[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}

/// <summary>
/// xUnit Collection Definition for PostgreSQL tests
/// All tests in this collection share the same PostgreSqlFixture instance
/// </summary>
[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}

/// <summary>
/// xUnit Collection Definition for InMemory tests
/// </summary>
[CollectionDefinition("InMemory")]
public class InMemoryCollection : ICollectionFixture<InMemoryFixture>
{
}
