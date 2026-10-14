using System.Collections.Generic;
using System.Globalization;
using S1API.PhoneApp;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LucroDoDia
{
    /// <summary>App "Lucro" no celular do jogo (a S1API instancia esta classe sozinha).</summary>
    public class ProfitApp : PhoneApp
    {
        public static ProfitApp Instance { get; private set; }

        private const string Green = "#7CE07C";
        private const string Red = "#FF7A7A";
        private const string Gray = "#B0B0B0";

        private Text _day;
        private Text _income;
        private Text _investment;
        private Text _profit;
        private Text _balance;
        private readonly List<Text> _historyLines = new List<Text>();

        protected override string AppName => "LucroDoDia";
        protected override string AppTitle => "Lucro do Dia";
        protected override string IconLabel => "Lucro";
        protected override string IconFileName => Core.IconFileName; // gravado em Mods/ pelo Core

        public ProfitApp()
        {
            Instance = this;
        }

        protected override void OnCreatedUI(GameObject container)
        {
            var root = UIFactory.Panel("LucroRoot", container.transform, new Color(0.09f, 0.11f, 0.10f), fullAnchor: true);
            UIFactory.VerticalLayoutOnGO(root, 6, new RectOffset(28, 28, 22, 22));
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            Line(root, "Lucro do Dia", 30, FontStyle.Bold);
            _day = Line(root, "", 18);
            Spacer(root, 8);
            _income = Line(root, "", 24);
            _investment = Line(root, "", 24);
            _profit = Line(root, "", 28, FontStyle.Bold);
            _balance = Line(root, "", 18);
            Spacer(root, 12);
            Line(root, "Ultimos dias", 20, FontStyle.Bold);
            for (var i = 0; i < DailyLedger.MaxHistoryDays; i++)
                _historyLines.Add(Line(root, "", 17));

            Refresh();
        }

        public void Refresh()
        {
            if (_day == null) return; // UI ainda nao foi criada

            var today = Core.Ledger.Today;
            _day.text = Colored(Gray, $"Dia {today.Day + 1} - atualiza sozinho");
            _income.text = $"Entradas:  {Colored(Green, "+" + Money(today.Income))}";
            _investment.text = $"Investimento:  {Colored(Red, "-" + Money(today.Investment))}";
            _profit.text = $"Lucro:  {Signed(today.Profit)}";
            _balance.text = Colored(Gray, Core.Ledger.Balance is double b
                ? $"Saldo (em maos + banco): {Money(b)}"
                : "Saldo: aguardando o jogo...");

            var history = Core.Ledger.History;
            for (var i = 0; i < _historyLines.Count; i++)
            {
                if (i < history.Count)
                {
                    var d = history[i];
                    _historyLines[i].text =
                        $"Dia {d.Day + 1}:  {Signed(d.Profit)}  {Colored(Gray, $"(+{Money(d.Income)} / -{Money(d.Investment)})")}";
                }
                else
                {
                    _historyLines[i].text = i == 0 && history.Count == 0 ? Colored(Gray, "Sem dias anteriores ainda.") : "";
                }
            }
        }

        private static Text Line(GameObject parent, string content, int size, FontStyle style = FontStyle.Normal)
        {
            var text = UIFactory.Text("Line", content, parent.transform, size, TextAnchor.MiddleLeft, style);
            text.supportRichText = true;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 10;
            return text;
        }

        private static void Spacer(GameObject parent, float height)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static string Money(double value) =>
            "$" + value.ToString("N0", CultureInfo.InvariantCulture);

        private static string Signed(double value) =>
            value >= 0 ? Colored(Green, "+" + Money(value)) : Colored(Red, "-" + Money(-value));

        private static string Colored(string hex, string text) => $"<color={hex}>{text}</color>";
    }
}
