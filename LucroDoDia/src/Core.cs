using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using S1API.GameTime;
using UnityEngine;

[assembly: MelonInfo(typeof(LucroDoDia.Core), LucroDoDia.Core.ModName, LucroDoDia.Core.ModVersion, "Fun-on-rio")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LucroDoDia
{
    public class Core : MelonMod
    {
        public const string ModName = "LucroDoDia";
        public const string ModVersion = "1.0.1";

        private const float PollIntervalSeconds = 0.5f;

        public static readonly DailyLedger Ledger = new DailyLedger();
        public static LedgerSave Save;

        private bool _inGame;
        private float _nextPoll;

        public const string IconFileName = "LucroDoDia_icon.png";

        public override void OnInitializeMelon()
        {
            EnsureIconFile();
            LoggerInstance.Msg("Carregado. Abra o app \"Lucro\" no celular do jogo.");
        }

        /// <summary>
        /// A S1API carrega o icone do app de Mods/&lt;arquivo&gt;. Gerenciadores de mods
        /// (r2modman/Thunderstore) nem sempre colocam um PNG solto ali, entao o icone
        /// vai embutido na DLL e e gravado na pasta Mods real ao iniciar.
        /// </summary>
        private void EnsureIconFile()
        {
            try
            {
                var target = Path.Combine(MelonEnvironment.ModsDirectory, IconFileName);
                using (var resource = typeof(Core).Assembly.GetManifestResourceStream("LucroDoDia.icon.png"))
                {
                    if (resource == null) return;
                    if (File.Exists(target) && new FileInfo(target).Length == resource.Length) return;
                    using (var file = File.Create(target))
                        resource.CopyTo(file);
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Warning($"Nao foi possivel gravar o icone do app: {e.Message}");
            }
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
