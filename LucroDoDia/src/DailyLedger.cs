using System;
using System.Collections.Generic;

namespace LucroDoDia
{
    /// <summary>Totais de um dia de jogo.</summary>
    public class DayRecord
    {
        public int Day;
        public double Income;      // tudo que entrou (vendas, dealers, lavagem...)
        public double Investment;  // tudo que saiu (sementes, ingredientes, equipamentos, contas...)
        public double Profit => Income - Investment;
    }

    /// <summary>
    /// Contabilidade do dia sem depender do jogo, para poder ser testada.
    ///
    /// Recebe amostras do saldo total (dinheiro em maos + banco) e classifica cada
    /// variacao: subiu = entrada, desceu = investimento. Como olha o TOTAL,
    /// depositar ou sacar no caixa eletronico nao conta como lucro nem gasto.
    /// </summary>
    public class DailyLedger
    {
        public const int MaxHistoryDays = 7;
        private const double Epsilon = 0.005; // ignora ruido de float

        private double? _lastBalance;

        public DayRecord Today { get; private set; }
        public List<DayRecord> History { get; private set; } = new List<DayRecord>(); // mais recente primeiro

        public DailyLedger(int day = 0)
        {
            Today = new DayRecord { Day = day };
        }

        /// <summary>Ultimo saldo observado (ou null antes da primeira amostra).</summary>
        public double? Balance => _lastBalance;

        public void Observe(double balance, int day)
        {
            if (day != Today.Day)
                RollOver(day);

            if (_lastBalance is double last)
            {
                var delta = balance - last;
                if (delta > Epsilon) Today.Income += delta;
                else if (delta < -Epsilon) Today.Investment += -delta;
            }
            // A primeira amostra so define a base: o saldo carregado do save nao e lucro.
            _lastBalance = balance;
        }

        /// <summary>Esquece a base de saldo (ex: ao sair para o menu), mantendo os totais.</summary>
        public void ResetBaseline() => _lastBalance = null;

        /// <summary>Restaura o estado salvo no save do jogo.</summary>
        public void Restore(DayRecord today, List<DayRecord> history)
        {
            Today = today ?? new DayRecord();
            History = history ?? new List<DayRecord>();
            _lastBalance = null;
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
        }
    }
}
