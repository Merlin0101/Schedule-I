using LucroDoDia;
using Xunit;

public class DailyLedgerTests
{
    [Fact]
    public void PrimeiraAmostraSoDefineABase()
    {
        var ledger = new DailyLedger(day: 3);
        ledger.Observe(5000, 3);

        Assert.Equal(0, ledger.Today.Income);
        Assert.Equal(0, ledger.Today.Investment);
        Assert.Equal(5000, ledger.Balance);
    }

    [Fact]
    public void SeparaEntradasDeInvestimento()
    {
        var ledger = new DailyLedger();
        ledger.Observe(1000, 0);
        ledger.Observe(1500, 0); // venda
        ledger.Observe(1200, 0); // compra de sementes
        ledger.Observe(1300, 0); // venda

        Assert.Equal(600, ledger.Today.Income);
        Assert.Equal(300, ledger.Today.Investment);
        Assert.Equal(300, ledger.Today.Profit);
    }

    [Fact]
    public void IgnoraRuidoDeFloat()
    {
        var ledger = new DailyLedger();
        ledger.Observe(100, 0);
        ledger.Observe(100.001, 0);

        Assert.Equal(0, ledger.Today.Income);
    }

    [Fact]
    public void ViradaDoDiaMoveParaOHistorico()
    {
        var ledger = new DailyLedger();
        ledger.Observe(100, 0);
        ledger.Observe(400, 0);
        ledger.Observe(350, 1); // ja no dia seguinte

        Assert.Equal(1, ledger.Today.Day);
        Assert.Equal(50, ledger.Today.Investment);
        var ontem = Assert.Single(ledger.History);
        Assert.Equal(0, ontem.Day);
        Assert.Equal(300, ontem.Profit);
    }

    [Fact]
    public void DiaVazioNaoEntraNoHistorico()
    {
        var ledger = new DailyLedger();
        ledger.Observe(100, 0);
        ledger.Observe(100, 1);

        Assert.Empty(ledger.History);
    }

    [Fact]
    public void HistoricoGuardaNoMaximoSeteDias()
    {
        var ledger = new DailyLedger();
        double saldo = 0;
        ledger.Observe(saldo, 0);
        for (var dia = 0; dia < 10; dia++)
        {
            saldo += 10;
            ledger.Observe(saldo, dia);
        }
        ledger.Observe(saldo, 10);

        Assert.Equal(DailyLedger.MaxHistoryDays, ledger.History.Count);
        Assert.Equal(9, ledger.History[0].Day); // mais recente primeiro
    }

    [Fact]
    public void ResetBaselineNaoContaOSaltoAoCarregarSave()
    {
        var ledger = new DailyLedger();
        ledger.Observe(100, 0);
        ledger.Observe(150, 0);
        ledger.ResetBaseline();
        ledger.Observe(99999, 0); // outro save carregado

        Assert.Equal(50, ledger.Today.Income);
        Assert.Equal(0, ledger.Today.Investment);
    }
}
