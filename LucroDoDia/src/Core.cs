using System;
using MelonLoader;
using S1API.GameTime;
using UnityEngine;

[assembly: MelonInfo(typeof(LucroDoDia.Core), LucroDoDia.Core.ModName, LucroDoDia.Core.ModVersion, "Fun-on-rio")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LucroDoDia
{
    public class Core : MelonMod
    {
        public const string ModName = "LucroDoDia";
        public const string ModVersion = "1.0.0";

        private const float PollIntervalSeconds = 0.5f;

        public static readonly DailyLedger Ledger = new DailyLedger();
        public static LedgerSave Save;

        private bool _inGame;
        private float _nextPoll;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Carregado. Abra o app \"Lucro\" no celular do jogo.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _inGame = sceneName == "Main";
            Ledger.ResetBaseline();

            if (!_inGame)
            {
                // Voltou ao menu: zera para nao vazar dados de um save para outro.
                Ledger.Clear();
                Save?.Capture(Ledger);
            }
        }

        public override void OnUpdate()
        {
            if (!_inGame || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + PollIntervalSeconds;

            double balance;
            int day;
            try
            {
                balance = S1API.Money.Money.GetCashBalance() + S1API.Money.Money.GetOnlineBalance();
                day = TimeManager.ElapsedDays;
            }
            catch (Exception)
            {
                return; // jogo ainda carregando (MoneyManager/TimeManager nao existem)
            }

            Ledger.Observe(balance, day);
            Save?.Capture(Ledger);
            ProfitApp.Instance?.Refresh();
        }
    }
}
