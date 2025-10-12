namespace MVFC.SQLCraft.Tests.CustomDriver;

public sealed class CustomFixture : IAsyncLifetime {
    private PostgreSqlContainer _container = default!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() {
        _container = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync() =>
        await _container.DisposeAsync();
}