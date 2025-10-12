namespace MVFC.SQLCraft.Tests.MsSQL;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private MsSqlContainer _container = default!;
    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}