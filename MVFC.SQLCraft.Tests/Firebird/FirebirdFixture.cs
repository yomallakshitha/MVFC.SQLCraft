namespace MVFC.SQLCraft.Tests.Firebird;

public sealed class FirebirdContainerFixture : IAsyncLifetime
{
    private FirebirdSqlContainer _container = default!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new FirebirdSqlBuilder()
            .WithImage("jacobalberty/firebird:latest")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}