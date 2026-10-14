using System;
using System.Collections.Generic;

namespace LucroDoDia
{
    /// <summary>Uma leitura de um dealer recrutado: ID do NPC e dinheiro guardado para o jogador.</summary>
    public struct DealerSample
    {
        public string Id;
        public double Cash;

        public DealerSample(string id, double cash)
        {
            Id = id;
            Cash = cash;
        }
    }

    /// <summary>Totais de um dia de jogo.</summary>
    public class DayRecord
    {
        public int Day;
        public double Income;        // tudo que entrou (vendas, dealers, lavagem...)
        public double Investment;    // tudo que saiu (sementes, ingredientes, equipamentos, contas...)
        public double DealerIncome;  // parte das entradas que veio de vendas dos dealers (ao vivo)
        public Dictionary<string, double> DealerSales = new Dictionary<string, double>(); // por ID do dealer
        public double TrackedMinutes; // minutos de jogo acompanhados hoje (base do lucro/hora)
        public bool GoalReached;     // ja avisou que bateu a meta hoje
        public double Profit => Income - Investment;

        /// <summary>Lucro por hora de jogo, ou null se ainda ha pouco tempo acompanhado.</summary>
        public double? ProfitPerHour =>
            TrackedMinutes >= DailyLedger.MinMinutesForRate ? Profit / (TrackedMinutes / 60.0) : (double?)null;
    }

    /// <summary>
    /// Contabilidade do dia sem depender do jogo, para poder ser testada.
    ///
    /// Recebe amostras do patrimonio liquido (dinheiro em maos + banco + dinheiro a
    /// coletar com os dealers) e classifica cada variacao: subiu = entrada, desceu =
    /// investimento. Como olha o TOTAL, depositar/sacar no caixa eletronico ou coletar
    /// o dinheiro de um dealer nao conta duas vezes: a venda do dealer entra na hora
    /// em que acontece, e a coleta e so uma transferencia.
    /// </summary>
    public class DailyLedger
    {
        public const int MaxHistoryDays = 7;
        public const double MinMinutesForRate = 30;
        private const double Epsilon = 0.005;     // ignora ruido de float
        private const int MaxMinuteStep = 120;    // saltos maiores (carregar save, dormir) nao contam como tempo jogado

        private double? _lastBalance;
        private readonly Dictionary<string, double> _lastDealerCash = new Dictionary<string, double>();
        private int? _lastMinute;

        public DayRecord Today { get; private set; }
        public List<DayRecord> History { get; private set; } = new List<DayRecord>(); // mais recente primeiro

        public DailyLedger(int day = 0)
        {
            Today = new DayRecord { Day = day };
        }

        /// <summary>Ultimo total observado, incluindo dealers (ou null antes da primeira amostra).</summary>
        public double? Balance => _lastBalance;

        /// <param name="balance">Dinheiro do jogador (em maos + banco).</param>
        /// <param name="minuteOfDay">Minutos desde 00:00 no relogio do jogo, ou -1 se desconhecido.</param>
        /// <param name="dealers">Dealers recrutados e o dinheiro que cada um guarda, ainda nao coletado.</param>
        public void Observe(double balance, int day, int minuteOfDay = -1, IReadOnlyList<DealerSample> dealers = null)
        {
            if (day != Today.Day)
                RollOver(day);

            if (dealers != null)
            {
                foreach (var dealer in dealers)
                {
                    if (string.IsNullOrEmpty(dealer.Id)) continue;
                    // Dinheiro do dealer subiu = ele vendeu. Desceu = voce coletou (transferencia).
                    if (_lastDealerCash.TryGetValue(dealer.Id, out var lastCash) && dealer.Cash - lastCash > Epsilon)
                    {
                        var sale = dealer.Cash - lastCash;
                        Today.DealerIncome += sale;
                        Today.DealerSales ??= new Dictionary<string, double>();
                        Today.DealerSales.TryGetValue(dealer.Id, out var sold);
                        Today.DealerSales[dealer.Id] = sold + sale;
                    }
                    _lastDealerCash[dealer.Id] = dealer.Cash;
                    balance += dealer.Cash;
                }
            }

            if (_lastBalance is double last)
            {
                var delta = balance - last;
                if (delta > Epsilon) Today.Income += delta;
                else if (delta < -Epsilon) Today.Investment += -delta;
            }
            // A primeira amostra so define a base: o saldo carregado do save nao e lucro.
            _lastBalance = balance;

            if (minuteOfDay >= 0)
            {
                if (_lastMinute is int prev)
                {
                    var step = minuteOfDay - prev;
                    if (step < 0) step += 24 * 60; // passou da meia-noite
                    if (step <= MaxMinuteStep) Today.TrackedMinutes += step;
                }
                _lastMinute = minuteOfDay;
            }
        }

        /// <summary>True uma unica vez por dia, quando o lucro alcanca a meta (meta &lt;= 0 desliga).</summary>
        public bool CheckGoal(double goal)
        {
            if (goal <= 0 || Today.GoalReached || Today.Profit < goal) return false;
            Today.GoalReached = true;
            return true;
        }

        /// <summary>Esquece a base de saldo/tempo (ex: ao sair para o menu), mantendo os totais.</summary>
        public void ResetBaseline()
        {
            _lastBalance = null;
            _lastDealerCash.Clear();
            _lastMinute = null;
        }

        /// <summary>Restaura o estado salvo no save do jogo.</summary>
        public void Restore(DayRecord today, List<DayRecord> history)
        {
            Today = today ?? new DayRecord();
            History = history ?? new List<DayRecord>();
            ResetBaseline();
        }

        public void Clear(int day = 0)
        {
            Restore(new DayRecord { Day = day }, new List<DayRecord>());
        }

        private void RollOver(int newDay)
        {
            // Dorme com o dia ainda vazio (ex: acabou de carregar) -> nao polui o historico.
            if (Today.Income > 0 || Today.Investment > 0)
            {
                History.Insert(0, Today);
                if (History.Count > MaxHistoryDays)
                    History.RemoveRange(MaxHistoryDays, History.Count - MaxHistoryDays);
            }
            Today = new DayRecord { Day = newDay };
            _lastMinute = null;
        }
    }

    /// <summary>Contas do grafico de barras (lucro pode ser negativo).</summary>
    public static class ChartMath
    {
        /// <summary>
        /// Altura da linha do zero (0..1) e a escala total, de forma que o maior
        /// lucro e o maior prejuizo caibam no grafico.
        /// </summary>
        public static (float baseline, double range) Scale(IList<double> values)
        {
            double maxPos = 0, maxNeg = 0;
            foreach (var v in values)
            {
                if (v > maxPos) maxPos = v;
                if (-v > maxNeg) maxNeg = -v;
            }
            var range = maxPos + maxNeg;
            if (range <= 0) return (0.5f, 1); // sem dados: zero no meio
            return ((float)(maxNeg / range), range);
        }

        /// <summary>Faixa vertical (0..1) ocupada por uma barra.</summary>
        public static (float bottom, float top) Bar(double value, float baseline, double range)
        {
            var size = (float)(Math.Abs(value) / range);
            return value >= 0 ? (baseline, baseline + size) : (baseline - size, baseline);
        }

        /// <summary>Vendas de hoje de um dealer (0 se nao vendeu).</summary>
        public static double SoldToday(DayRecord day, string dealerId) =>
            day?.DealerSales != null && dealerId != null && day.DealerSales.TryGetValue(dealerId, out var v) ? v : 0;

        /// <summary>Formato curto para rotulos: $950, $12.3k, $4.2M.</summary>
        public static string Compact(double value)
        {
            var sign = value < 0 ? "-" : "";
            var abs = Math.Abs(value);
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (abs >= 1_000_000) return sign + "$" + (abs / 1_000_000).ToString("0.#", inv) + "M";
            if (abs >= 1_000) return sign + "$" + (abs / 1_000).ToString("0.#", inv) + "k";
            return sign + "$" + abs.ToString("0", inv);
        }
    }
}
