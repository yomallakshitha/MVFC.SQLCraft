namespace MVFC.SQLCraft.Tests.MsSQL;

[Collection("SqlServer")]
public class SqlKataHelperSqlServerIntegrationTests(SqlServerContainerFixture fixture) : IClassFixture<SqlServerContainerFixture> {
    private readonly MsSQLCraftDriver _driver = new(fixture.ConnectionString);

    [Fact]
    public void Testar_Insert_E_Select_PorId() {
        _driver.Execute("IF OBJECT_ID('dbo.Persons', 'U') IS NULL CREATE TABLE Persons (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(100) NOT NULL);");

        var insertQ = new Query("Persons").AsInsert(new { Name = "Alice" });
        var affected = _driver.Execute(insertQ);
        Assert.Equal(1, affected);

        var selQ = new Query("Persons").Select("Id", "Name").Where("Name", "Alice");
        var person = _driver.QueryFirstOrDefault<Person>(selQ);

        Assert.NotNull(person);
        Assert.Equal("Alice", person!.Name);
        Assert.True(person.Id > 0);
    }

    [Fact]
    public async Task Testar_Update_E_Select() {
        await _driver.ExecuteAsync("IF OBJECT_ID('dbo.Persons2', 'U') IS NULL CREATE TABLE Persons2 (Id INT IDENTITY(1,1) PRIMARY KEY, Name NVARCHAR(100) NOT NULL);");

        await _driver.ExecuteInTransactionAsync(async (x, t, ct) => {
            await x.ExecuteAsync(new Query("Persons2").AsInsert(new { Name = "Bob" }), t, ct);

            var sel = new Query("Persons2").Select("Id", "Name").Where("Name", "Bob");
            var p = await x.QueryFirstOrDefaultAsync<Person>(sel, t, ct);
            Assert.NotNull(p);

            var upd = new Query("Persons2").Where("Id", p!.Id).AsUpdate(new { Name = "Robert" });
            var upaff = await x.ExecuteAsync(upd, t, ct);
            Assert.Equal(1, upaff);

            var sel2 = new Query("Persons2").Select("Id", "Name").Where("Id", p.Id);
            var p2 = await x.QueryFirstOrDefaultAsync<Person>(sel2, t, ct);
            Assert.NotNull(p2);
            Assert.Equal("Robert", p2!.Name);
        });
    }
}