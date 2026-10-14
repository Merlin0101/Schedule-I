using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S1API.Utils;
using S1API.PhoneApp;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LucroDoDia
{
    /// <summary>
    /// App "Lucro" no celular do jogo (a S1API instancia esta classe sozinha).
    /// Esquerda: numeros do dia. Direita: grafico de lucro dos ultimos 7 dias.
    /// </summary>
    public class ProfitApp : PhoneApp
    {
        public static ProfitApp Instance { get; private set; }

        private const int ChartSlots = 7; // 6 dias anteriores + hoje
        private const int RankingRows = 8;

        private const string GreenHex = "#7CE07C";
        private const string RedHex = "#FF7A7A";
        private const string GrayHex = "#B0B0B0";
        private static readonly Color Background = new Color(0.09f, 0.11f, 0.10f);
        private static readonly Color Clear = new Color(0, 0, 0, 0);
        private static readonly Color ChartBg = new Color(1, 1, 1, 0.04f);
        private static readonly Color Green = new Color(0.39f, 0.80f, 0.43f);
        private static readonly Color GreenToday = new Color(0.55f, 0.93f, 0.58f);
        private static readonly Color Red = new Color(0.95f, 0.42f, 0.42f);
        private static readonly Color RedToday = new Color(1f, 0.58f, 0.58f);
        private static readonly Color Gold = new Color(1f, 0.84f, 0.31f);
        private static readonly Color TabOn = new Color(0.30f, 0.62f, 0.36f);
        private static readonly Color TabOff = new Color(1, 1, 1, 0.08f);
        private const string GoldHex = "#FFD54F";

        private Text _day, _income, _dealers, _investment, _profit, _perHour, _goal, _cash, _bank, _dealerCash;
        private RectTransform _goalFill;
        private GameObject _goalBar;
        private RectTransform _zeroLine;
        private readonly List<RectTransform> _bars = new List<RectTransform>();
        private readonly List<Image> _barImages = new List<Image>();
        private readonly List<Text> _barValues = new List<Text>();
        private readonly List<Text> _barDays = new List<Text>();

        private GameObject _chartGroup, _rankingGroup;
        private Image _chartTab, _rankingTab;
        private bool _showRanking;
        private Text _rankingHeader;
        private readonly List<Text> _rankingLines = new List<Text>();

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
            var root = UIFactory.Panel("LucroRoot", container.transform, Background, fullAnchor: true);
            BuildStats(root);
            BuildChart(root);
            ShowRanking(false); // tambem faz o primeiro Refresh, com tudo ja criado
        }

        // ---------- coluna da esquerda ----------

        private void BuildStats(GameObject root)
        {
            var left = UIFactory.Panel("Stats", root.transform, Clear);
            Stretch(left, 0f, 0f, 0.47f, 1f);
            UIFactory.VerticalLayoutOnGO(left, 3, new RectOffset(26, 8, 18, 14));
            var layout = left.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            Line(left, "Lucro do Dia", 28, FontStyle.Bold);
            _day = Line(left, "", 16);
            Spacer(left, 6);
            _income = Line(left, "", 21);
            _dealers = Line(left, "", 15);
            _investment = Line(left, "", 21);
            _profit = Line(left, "", 26, FontStyle.Bold);
            _perHour = Line(left, "", 17);
            Spacer(left, 6);
            _goal = Line(left, "", 17);
            BuildGoalBar(left);
            Spacer(left, 8);
            _cash = Line(left, "", 16);
            _bank = Line(left, "", 16);
            _dealerCash = Line(left, "", 16);
        }

        private void BuildGoalBar(GameObject parent)
        {
            _goalBar = UIFactory.Panel("GoalBar", parent.transform, new Color(1, 1, 1, 0.10f));
            _goalBar.AddComponent<LayoutElement>().preferredHeight = 12;
            var fill = UIFactory.Panel("GoalFill", _goalBar.transform, Gold);
            _goalFill = Stretch(fill, 0f, 0f, 0f, 1f);
        }

        // ---------- coluna da direita (grafico) ----------

        private void BuildChart(GameObject root)
        {
            var column = UIFactory.Panel("Right", root.transform, Clear);
            Stretch(column, 0.49f, 0f, 1f, 1f);

            // Grupos primeiro e sem bloquear clique; abas por ultimo, para ficarem por cima.
            _chartGroup = UIFactory.Panel("ChartGroup", column.transform, Clear);
            Stretch(_chartGroup, 0f, 0f, 1f, 1f);
            _chartGroup.GetComponent<Image>().raycastTarget = false;
            _rankingGroup = UIFactory.Panel("RankingGroup", column.transform, Clear);
            Stretch(_rankingGroup, 0f, 0f, 1f, 1f);
            _rankingGroup.GetComponent<Image>().raycastTarget = false;
            BuildRanking(_rankingGroup);

            _chartTab = Tab(column, "Últimos 7 dias", 0.02f, 0.48f, () => ShowRanking(false));
            _rankingTab = Tab(column, "Dealers", 0.50f, 0.96f, () => ShowRanking(true));

            var right = _chartGroup;
            var area = UIFactory.Panel("ChartArea", right.transform, ChartBg);
            Stretch(area, 0.02f, 0.14f, 0.96f, 0.86f);

            _zeroLine = Stretch(UIFactory.Panel("Zero", area.transform, new Color(1, 1, 1, 0.35f)), 0f, 0.5f, 1f, 0.5f);

            const float gap = 0.18f; // espaco entre barras, em fracao da coluna
            for (var i = 0; i < ChartSlots; i++)
            {
                var x0 = (i + gap / 2) / ChartSlots;
                var x1 = (i + 1 - gap / 2) / ChartSlots;

                var bar = UIFactory.Panel("Bar" + i, area.transform, Green);
                _bars.Add(Stretch(bar, x0, 0.5f, x1, 0.5f));
                _barImages.Add(bar.GetComponent<Image>());

                var value = UIFactory.Text("Value" + i, "", area.transform, 12, TextAnchor.MiddleCenter);
                _barValues.Add(value);

                // rotulo do dia fica fora da area do grafico, embaixo
                var ax0 = 0.02f + x0 * 0.94f;
                var ax1 = 0.02f + x1 * 0.94f;
                var day = UIFactory.Text("Day" + i, "", right.transform, 12, TextAnchor.MiddleCenter);
                Stretch(day.gameObject, ax0 - 0.02f, 0.04f, ax1 + 0.02f, 0.13f);
                _barDays.Add(day);
            }
        }

        private void BuildRanking(GameObject group)
        {
            var list = UIFactory.Panel("RankingList", group.transform, ChartBg);
            Stretch(list, 0.02f, 0.04f, 0.96f, 0.86f);
            UIFactory.VerticalLayoutOnGO(list, 2, new RectOffset(12, 10, 8, 8));
            var layout = list.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            _rankingHeader = Line(list, "", 13);
            for (var i = 0; i < RankingRows; i++)
                _rankingLines.Add(Line(list, "", 15));
        }

        private Image Tab(GameObject parent, string label, float x0, float x1, System.Action onClick)
        {
            var (go, button, text) = UIFactory.ButtonWithLabel("Tab" + label, label, parent.transform, TabOff, 10, 10);
            Stretch(go, x0, 0.88f, x1, 0.98f);
            text.fontSize = 15;
            EventHelper.AddListener(onClick, button.onClick);
            return go.GetComponent<Image>();
        }

        private void ShowRanking(bool show)
        {
            _showRanking = show;
            _chartGroup.SetActive(!show);
            _rankingGroup.SetActive(show);
            _chartTab.color = show ? TabOff : TabOn;
            _rankingTab.color = show ? TabOn : TabOff;
            Refresh();
        }

        // ---------- atualizacao ----------

        public void Refresh()
        {
            if (_day == null) return; // UI ainda nao foi criada

            var ledger = Core.Ledger;
            var today = ledger.Today;

            _day.text = Colored(GrayHex, $"Dia {today.Day + 1} · atualiza sozinho");
            _income.text = $"Entradas:  {Colored(GreenHex, "+" + Money(today.Income))}";
            _dealers.text = Core.RecruitedDealers > 0
                ? Colored(GrayHex, $"   dos dealers: +{Money(today.DealerIncome)}")
                : "";
            _investment.text = $"Investimento:  {Colored(RedHex, "-" + Money(today.Investment))}";
            _profit.text = $"Lucro:  {Signed(today.Profit)}";
            _perHour.text = today.ProfitPerHour is double rate
                ? $"Por hora de jogo:  {Signed(rate)}"
                : Colored(GrayHex, "Por hora de jogo: calculando...");

            RefreshGoal(today);

            var known = ledger.Balance.HasValue;
            _cash.text = Colored(GrayHex, known ? $"Em mãos: {Money(Core.Cash)}" : "Saldo: aguardando o jogo...");
            _bank.text = Colored(GrayHex, known ? $"No banco: {Money(Core.Bank)}" : "");
            _dealerCash.text = known && Core.RecruitedDealers > 0
                ? Colored(GrayHex, $"Com os dealers ({Core.RecruitedDealers}): {Money(Core.DealerCash)} a coletar")
                : "";

            if (_showRanking) RefreshRanking(today);
            else RefreshChart(ledger);
        }

        private void RefreshRanking(DayRecord today)
        {
            var dealers = DealerTracker.Current
                .OrderByDescending(d => ChartMath.SoldToday(today, d.Id))
                .ThenByDescending(d => d.Cash)
                .ToList();

            _rankingHeader.text = dealers.Count == 0
                ? Colored(GrayHex, "Nenhum dealer recrutado ainda.")
                : Colored(GrayHex, "Vendeu hoje · a coletar");

            for (var i = 0; i < _rankingLines.Count; i++)
            {
                if (i >= dealers.Count)
                {
                    _rankingLines[i].text = "";
                    continue;
                }
                if (i == _rankingLines.Count - 1 && dealers.Count > _rankingLines.Count)
                {
                    _rankingLines[i].text = Colored(GrayHex, $"+{dealers.Count - i} outros");
                    continue;
                }

                var d = dealers[i];
                var sold = ChartMath.SoldToday(today, d.Id);
                var soldText = sold > 0 ? Colored(GreenHex, "+" + ChartMath.Compact(sold)) : Colored(GrayHex, "$0");
                var cashText = d.Cash >= 1 ? Colored(GoldHex, ChartMath.Compact(d.Cash)) : Colored(GrayHex, "$0");
                _rankingLines[i].text = $"{i + 1}. {Shorten(d.Name, 16)}  {soldText} · {cashText}";
            }
        }

        private static string Shorten(string name, int max) =>
            string.IsNullOrEmpty(name) || name.Length <= max ? name : name.Substring(0, max - 1) + "…";

        private void RefreshGoal(DayRecord today)
        {
            var goal = Core.DailyGoal;
            var hasGoal = goal > 0;
            _goalBar.SetActive(hasGoal);
            if (!hasGoal)
            {
                _goal.text = Colored(GrayHex, "Meta do dia: desligada (MetaDiaria = 0)");
                return;
            }

            var progress = Mathf.Clamp01((float)(today.Profit / goal));
            _goalFill.anchorMax = new Vector2(progress, 1f);
            _goal.text = today.Profit >= goal
                ? $"Meta {Money(goal)}:  {Colored(GreenHex, "batida!")}"
                : $"Meta {Money(goal)}:  {Colored(GrayHex, $"{progress * 100:0}% · faltam {Money(goal - today.Profit)}")}";
        }

        private void RefreshChart(DailyLedger ledger)
        {
            // Mais antigo a esquerda, hoje na ultima coluna.
            var days = new List<DayRecord>();
            for (var i = Mathf.Min(ChartSlots - 1, ledger.History.Count) - 1; i >= 0; i--)
                days.Add(ledger.History[i]);
            days.Add(ledger.Today);

            var profits = new List<double>();
            foreach (var d in days) profits.Add(d.Profit);
            var (baseline, range) = ChartMath.Scale(profits);
            _zeroLine.anchorMin = new Vector2(0f, baseline);
            _zeroLine.anchorMax = new Vector2(1f, baseline);
            _zeroLine.offsetMin = new Vector2(0f, -1f);
            _zeroLine.offsetMax = new Vector2(0f, 1f);

            var offset = ChartSlots - days.Count; // colunas vazias ficam a esquerda
            for (var slot = 0; slot < ChartSlots; slot++)
            {
                var bar = _bars[slot];
                var value = _barValues[slot];
                var label = _barDays[slot];
                var index = slot - offset;

                if (index < 0)
                {
                    bar.gameObject.SetActive(false);
                    value.text = "";
                    label.text = "";
                    continue;
                }

                var day = days[index];
                var isToday = index == days.Count - 1;
                var (bottom, top) = ChartMath.Bar(day.Profit, baseline, range);

                bar.gameObject.SetActive(true);
                bar.anchorMin = new Vector2(bar.anchorMin.x, bottom);
                bar.anchorMax = new Vector2(bar.anchorMax.x, top);
                bar.offsetMin = Vector2.zero;
                bar.offsetMax = new Vector2(0f, top - bottom < 0.005f ? 2f : 0f); // dia zerado ainda aparece como tracinho
                _barImages[slot].color = day.Profit >= 0
                    ? (isToday ? GreenToday : Green)
                    : (isToday ? RedToday : Red);

                // valor logo acima (lucro) ou abaixo (prejuizo) da barra
                var vy0 = day.Profit >= 0 ? top : Mathf.Max(0f, bottom - 0.11f);
                var vy1 = day.Profit >= 0 ? Mathf.Min(1f, top + 0.11f) : bottom;
                var rt = value.rectTransform;
                rt.anchorMin = new Vector2(bar.anchorMin.x - 0.03f, vy0);
                rt.anchorMax = new Vector2(bar.anchorMax.x + 0.03f, vy1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                value.text = ChartMath.Compact(day.Profit);

                label.text = isToday ? "<b>Hoje</b>" : $"Dia {day.Day + 1}";
                label.supportRichText = true;
            }
        }

        // ---------- helpers de UI ----------

        private static RectTransform Stretch(GameObject go, float x0, float y0, float x1, float y1)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Text Line(GameObject parent, string content, int size, FontStyle style = FontStyle.Normal)
        {
            var text = UIFactory.Text("Line", content, parent.transform, size, TextAnchor.MiddleLeft, style);
            text.supportRichText = true;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 8;
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
            value >= 0 ? Colored(GreenHex, "+" + Money(value)) : Colored(RedHex, "-" + Money(-value));

        private static string Colored(string hex, string text) => $"<color={hex}>{text}</color>";
    }
}
