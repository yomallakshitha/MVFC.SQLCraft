namespace MVFC.SQLCraft.Tests.Mysql;

public sealed class MySqlContainerFixture : IAsyncLifetime
{
    private MySqlContainer _container = default!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new MySqlBuilder()
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}