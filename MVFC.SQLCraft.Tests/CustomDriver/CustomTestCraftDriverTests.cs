namespace MVFC.SQLCraft.Tests.CustomDriver;

public sealed class CustomTestCraftDriverTests(CustomFixture fixture) : IClassFixture<CustomFixture> {
    private readonly CustomFixture _fixture = fixture;
    private readonly CustomTestCraftDriver _driver = new(fixture.ConnectionString);

    private CustomTestCraftDriver CriarDriver(CustomTestLogger? logger = null)
        => new(_fixture.ConnectionString, logger);

    [Fact]
    public void ExecutarEmTransacao_Confirma_QuandoSemExcecao() {
        _driver.Execute("CREATE TABLE IF NOT EXISTS tx_test (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        _driver.ExecuteInTransaction((drv, tx) => drv.Execute(new Query("tx_test").AsInsert(new { name = "TxCommit" }), tx));

        var person = _driver.QueryFirstOrDefault<Person>(new Query("tx_test").Where("name", "TxCommit"));
        Assert.NotNull(person);
        Assert.Equal("TxCommit", person!.Name);
    }

    [Fact]
    public void ExecutarEmTransacao_Desfaz_QuandoExcecao() {
        _driver.Execute("CREATE TABLE IF NOT EXISTS tx_test2 (id SERIAL PRIMARY KEY, name VARCHAR(100));");

        Assert.Throws<InvalidOperationException>(() => {
            _driver.ExecuteInTransaction((drv, tx) => {
                drv.Execute(new Query("tx_test2").AsInsert(new { name = "TxRollback" }), tx);
                throw new InvalidOperationException();
            });
        });

        var person = _driver.QueryFirstOrDefault<Person>(new Query("tx_test2").Where("name", "TxRollback"));
        Assert.Null(person);
    }

    [Fact]
    public async Task ExecutarEmTransacaoAsync_Confirma_QuandoSemExcecao() {
        await _driver.ExecuteAsync("CREATE TABLE IF NOT EXISTS tx_test_async (id SERIAL PRIMARY KEY, name VARCHAR(100));");

        await _driver.ExecuteInTransactionAsync(async (drv, tx, ct) =>
            await drv.ExecuteAsync(new Query("tx_test_async").AsInsert(new { name = "TxCommitAsync" }), tx, ct));

        var person = await _driver.QueryFirstOrDefaultAsync<Person>(new Query("tx_test_async").Where("name", "TxCommitAsync"));

        Assert.NotNull(person);
        Assert.Equal("TxCommitAsync", person!.Name);
    }

    [Fact]
    public async Task ExecutarEmTransacaoAsync_Desfaz_QuandoExcecao() {
        await _driver.ExecuteAsync("CREATE TABLE IF NOT EXISTS tx_test_async2 (id SERIAL PRIMARY KEY, name VARCHAR(100));");

        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await _driver.ExecuteInTransactionAsync(async (drv, tx, ct) => {
                await drv.ExecuteAsync(new Query("tx_test_async2").AsInsert(new { name = "TxRollbackAsync" }), tx, ct);
                throw new InvalidOperationException();
            });
        });

        var person = await _driver.QueryFirstOrDefaultAsync<Person>(new Query("tx_test_async2").Where("name", "TxRollbackAsync"));
        Assert.Null(person);
    }

    [Fact]
    public void ExecutarSqlDireto_Funciona() {
        var affected = _driver.Execute("CREATE TABLE IF NOT EXISTS direct_sql_test (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        Assert.Equal(-1, affected);
    }

    [Fact]
    public async Task ExecutarSqlDiretoAsync_Funciona() {
        var affected = await _driver.ExecuteAsync("CREATE TABLE IF NOT EXISTS direct_sql_test_async (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        Assert.Equal(-1, affected);
    }

    [Fact]
    public void Consultar_LancaELogaErro_QuandoTabelaNaoExiste() {
        var query = new Query("table_inexistente").Select("id", "name");
        Assert.ThrowsAny<Exception>(() => _driver.Query<Person>(query));
    }

    [Fact]
    public async Task ConsultarAsync_LancaELogaErro_QuandoTabelaNaoExiste() {
        var query = new Query("table_inexistente_async").Select("id", "name");
        await Assert.ThrowsAnyAsync<Exception>(async () => await _driver.QueryAsync<Person>(query));
    }

    [Fact]
    public void Executar_LancaELogaErro_QuandoSqlInvalido() =>
        Assert.ThrowsAny<Exception>(() => _driver.Execute("INVALID SQL COMMAND"));

    [Fact]
    public async Task ExecutarAsync_LancaELogaErro_QuandoSqlInvalido() =>
        await Assert.ThrowsAnyAsync<Exception>(async () => await _driver.ExecuteAsync("INVALID SQL COMMAND"));

    [Fact]
    public void DisposeConexao_Chamado_EmConsulta() {
        var driver = CriarDriver();
        Assert.ThrowsAny<Exception>(() => driver.Query<Person>(new Query("table_inexistente")));
        Assert.True(driver.DisposeConnCalled);
    }

    [Fact]
    public async Task DisposeConexaoAsync_Chamado_EmConsultaAsync() {
        var driver = CriarDriver();
        await Assert.ThrowsAnyAsync<Exception>(async () => await driver.QueryAsync<Person>(new Query("table_inexistente_async")));
        Assert.True(driver.DisposeConnAsyncCalled);
    }

    [Fact]
    public void LogAntesEDepois_Chamados_EmExecucao() {
        var driver = CriarDriver();
        driver.Execute("CREATE TABLE IF NOT EXISTS log_test_full (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        driver.Execute(new Query("log_test_full").AsInsert(new { name = "TestFull" }));
        Assert.True(driver.LogBeforeCalled);
        Assert.True(driver.LogAfterCalled);
    }

    [Fact]
    public void LogErro_Chamado_EmErroExecucao() {
        var driver = CriarDriver();
        Assert.ThrowsAny<Exception>(() => driver.Execute("INVALID SQL"));
        Assert.True(driver.LogErrorCalled);
    }

    [Fact]
    public void LogErro_Chamado_EmExcecaoConsulta() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);

        var query = new Query("table_inexistente").Select("id", "name");
        Assert.ThrowsAny<Exception>(() => driver.Query<Person>(query));
        Assert.True(logger.ErrorCalled);
        Assert.NotNull(logger.LastException);
        Assert.NotNull(logger.LastSql);
    }

    [Fact]
    public async Task LogErroAsync_Chamado_EmExcecaoConsultaAsync() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);

        var query = new Query("table_inexistente_async").Select("id", "name");
        await Assert.ThrowsAnyAsync<Exception>(async () => await driver.QueryAsync<Person>(query));
        Assert.True(logger.ErrorCalled);
        Assert.NotNull(logger.LastException);
        Assert.NotNull(logger.LastSql);
    }

    [Fact]
    public void LogAntesEDepois_Chamados_ComSucesso() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);

        driver.Execute("CREATE TABLE IF NOT EXISTS log_test (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        var query = new Query("log_test").AsInsert(new { name = "Test" });
        driver.Execute(query);

        Assert.True(logger.BeforeCalled);
        Assert.True(logger.AfterCalled);
    }

    [Fact]
    public async Task LogAntesEDepoisAsync_Chamados_ComSucesso() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);

        await driver.ExecuteAsync("CREATE TABLE IF NOT EXISTS log_test_async (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        var query = new Query("log_test_async").AsInsert(new { name = "TestAsync" });
        await driver.ExecuteAsync(query);

        Assert.True(logger.BeforeCalled);
        Assert.True(logger.AfterCalled);
    }

    [Fact]
    public async Task ConsultarPrimeiroOuPadrao_Deve_LogarErro_EmExcecao() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);

        var query = new Query("tabela");
        driver.Execute("CREATE TABLE IF NOT EXISTS tabela (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        driver.ThrowInternalError = true;

        Assert.Throws<InvalidOperationException>(() => driver.Execute("SELECT 1"));
        Assert.Throws<InvalidOperationException>(() => driver.QueryFirstOrDefault<object>(query));
        Assert.Throws<InvalidOperationException>(() => driver.Query<object>(query));
        Assert.Throws<InvalidOperationException>(() => driver.Execute(query));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await driver.QueryFirstOrDefaultAsync<object>(query));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await driver.QueryAsync<object>(query));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await driver.ExecuteAsync(query));
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await driver.ExecuteAsync("SELECT 1"));

        Assert.True(logger.ErrorCalled);
    }

    [Fact]
    public void Consultar_ComTransacao_CobreBranchTx() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);
        var query = new Query("tabela1");

        driver.Execute("CREATE TABLE IF NOT EXISTS tabela1 (id SERIAL PRIMARY KEY, name VARCHAR(100));");
        driver.ExecuteInTransaction((drv, tx) => {
            drv.Query<object>(query, tx);
            drv.Execute(query, tx);
            drv.Execute("SELECT 1", tx);
            drv.QueryFirstOrDefault<object>(query, tx);
        });

        Assert.True(driver.LogAfterCalled);
    }

    [Fact]
    public async Task ConsultarAsync_ComTransacao_CobreBranchTx() {
        var logger = new CustomTestLogger();
        var driver = CriarDriver(logger);
        var query = new Query("table2");

        await driver.ExecuteAsync("CREATE TABLE IF NOT EXISTS table2 (id SERIAL PRIMARY KEY, name VARCHAR(100));");

        await driver.ExecuteInTransactionAsync(async (drv, tx, ct) => {
            await drv.QueryAsync<object>(query, tx, ct);
            await drv.ExecuteAsync(query, tx, ct);
            await drv.QueryFirstOrDefaultAsync<object>(query, tx, ct);
            await drv.ExecuteAsync("SELECT 1", tx, ct);
        });

        Assert.True(logger.AfterCalled);
    }
}