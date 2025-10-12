namespace MVFC.SQLCraft.Tests.SQLite;

public sealed class SQLiteFixture : IAsyncLifetime
{
    private string _databasePath = default!;

    public string ConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        _databasePath = Path.GetTempFileName() + ".sqlite";
        ConnectionString = $"Data Source={_databasePath};Version=3;";

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);

        await Task.CompletedTask;
    }
}