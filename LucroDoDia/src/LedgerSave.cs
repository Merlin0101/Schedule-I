using System.Collections.Generic;
using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace LucroDoDia
{
    /// <summary>
    /// Guarda o dia atual e o historico dentro do proprio save do jogo
    /// (a S1API descobre e instancia esta classe automaticamente).
    /// </summary>
    public class LedgerSave : Saveable
    {
        [SaveableField("today")]
        private DayRecord _today;

        [SaveableField("history")]
        private List<DayRecord> _history;

        public LedgerSave()
        {
            Core.Save = this;
        }

        protected override void OnLoaded()
        {
            Core.Ledger.Restore(_today, _history);
        }

        /// <summary>Copia o estado atual do ledger para os campos gravados (chamado a cada leitura de saldo).</summary>
        public void Capture(DailyLedger ledger)
        {
            _today = ledger.Today;
            _history = ledger.History;
        }
    }
}
