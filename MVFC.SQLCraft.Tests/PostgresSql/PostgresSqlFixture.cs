namespace MVFC.SQLCraft.Tests.PostgresSql;

public sealed class PostgresSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = default!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}