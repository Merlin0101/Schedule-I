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

public class LucroPorHoraEMetaTests
{
    [Fact]
    public void ContaMinutosDeJogoEDivideOLucro()
    {
        var ledger = new DailyLedger();
        ledger.Observe(0, 0, 8 * 60);
        ledger.Observe(300, 0, 9 * 60);     // +$300 em 1h
        ledger.Observe(600, 0, 10 * 60);    // +$300 em mais 1h

        Assert.Equal(120, ledger.Today.TrackedMinutes);
        Assert.Equal(300, ledger.Today.ProfitPerHour);
    }

    [Fact]
    public void PoucoTempoAindaNaoTemTaxa()
    {
        var ledger = new DailyLedger();
        ledger.Observe(0, 0, 600);
        ledger.Observe(100, 0, 610);

        Assert.Null(ledger.Today.ProfitPerHour);
    }

    [Fact]
    public void PassarDaMeiaNoiteContaNormalESaltoGrandeNao()
    {
        var ledger = new DailyLedger();
        ledger.Observe(0, 0, 23 * 60 + 50);
        ledger.Observe(0, 0, 10);          // 20 min depois, ja depois da meia-noite
        ledger.Observe(0, 0, 5 * 60);      // salto de quase 5h (ex: carregou o save) - ignora

        Assert.Equal(20, ledger.Today.TrackedMinutes);
    }

    [Fact]
    public void MetaAvisaUmaVezSo()
    {
        var ledger = new DailyLedger();
        ledger.Observe(0, 0);
        ledger.Observe(900, 0);
        Assert.False(ledger.CheckGoal(1000));

        ledger.Observe(1100, 0);
        Assert.True(ledger.CheckGoal(1000));
        Assert.False(ledger.CheckGoal(1000));

        ledger.Observe(1100, 1);           // dia novo: pode avisar de novo
        ledger.Observe(2200, 1);
        Assert.True(ledger.CheckGoal(1000));
    }

    [Fact]
    public void MetaZeroDesliga()
    {
        var ledger = new DailyLedger();
        ledger.Observe(0, 0);
        ledger.Observe(1_000_000, 0);
        Assert.False(ledger.CheckGoal(0));
    }
}

public class ChartMathTests
{
    [Fact]
    public void SoLucroDeixaOZeroNoChao()
    {
        var (baseline, range) = ChartMath.Scale(new double[] { 100, 400, 200 });
        Assert.Equal(0f, baseline);
        Assert.Equal(400, range);
        Assert.Equal((0f, 1f), ChartMath.Bar(400, baseline, range));
    }

    [Fact]
    public void LucroEPrejuizoDividemOEspaco()
    {
        var (baseline, range) = ChartMath.Scale(new double[] { 300, -100 });
        Assert.Equal(0.25f, baseline);
        var (bottom, top) = ChartMath.Bar(-100, baseline, range);
        Assert.Equal(0f, bottom, 3);
        Assert.Equal(0.25f, top, 3);
    }

    [Fact]
    public void SemDadosZeroNoMeio()
    {
        Assert.Equal(0.5f, ChartMath.Scale(new double[] { 0, 0 }).baseline);
    }

    [Theory]
    [InlineData(950, "$950")]
    [InlineData(12345, "$12.3k")]
    [InlineData(-4_250_000, "-$4.3M")]
    public void FormatoCurto(double value, string expected)
    {
        Assert.Equal(expected, ChartMath.Compact(value));
    }
}

public class DealerTests
{
    [Fact]
    public void VendaDoDealerEntraNaHora()
    {
        var ledger = new DailyLedger();
        ledger.Observe(1000, 0, -1, dealerPool: 0);
        ledger.Observe(1000, 0, -1, dealerPool: 400); // dealer vendeu, voce ainda nao coletou

        Assert.Equal(400, ledger.Today.Income);
        Assert.Equal(400, ledger.Today.DealerIncome);
    }

    [Fact]
    public void ColetarODinheiroNaoContaDuasVezes()
    {
        var ledger = new DailyLedger();
        ledger.Observe(1000, 0, -1, dealerPool: 0);
        ledger.Observe(1000, 0, -1, dealerPool: 400);
        ledger.Observe(1400, 0, -1, dealerPool: 0);   // coletou: sai do dealer, entra em maos

        Assert.Equal(400, ledger.Today.Income);
        Assert.Equal(0, ledger.Today.Investment);
        Assert.Equal(400, ledger.Today.DealerIncome);
    }

    [Fact]
    public void ResetDeBaseIgnoraDealerRecemCarregado()
    {
        var ledger = new DailyLedger();
        ledger.Observe(1000, 0, -1, dealerPool: 0);
        ledger.ResetBaseline();                        // dealers terminaram de carregar
        ledger.Observe(1000, 0, -1, dealerPool: 5000);

        Assert.Equal(0, ledger.Today.Income);
        Assert.Equal(0, ledger.Today.DealerIncome);
    }

    [Fact]
    public void VendasDoDealerViramDiaComOResto()
    {
        var ledger = new DailyLedger();
        ledger.Observe(0, 0, -1, dealerPool: 0);
        ledger.Observe(0, 0, -1, dealerPool: 250);
        ledger.Observe(0, 1, -1, dealerPool: 250);

        Assert.Equal(250, Assert.Single(ledger.History).DealerIncome);
        Assert.Equal(0, ledger.Today.DealerIncome);
    }
}
