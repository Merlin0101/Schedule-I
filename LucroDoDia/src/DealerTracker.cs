using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Entities;

namespace LucroDoDia
{
    /// <summary>
    /// Soma o dinheiro que os dealers recrutados estao guardando para o jogador
    /// (Dealer.Cash: parte do jogador depois do corte do dealer, ainda nao coletada).
    ///
    /// Le o componente Dealer direto, por reflection: os metodos GetCash/IsRecruited
    /// da S1API chamam EnsureDealer() a cada uso, o que re-inicializa o dealer e
    /// escreve avisos no log para NPCs que nao sao dealers.
    /// </summary>
    internal static class DealerTracker
    {
        private const float RefreshListSeconds = 10f;

        /// <summary>Dealer recrutado com nome e dinheiro a coletar (ultima leitura).</summary>
        public class Info
        {
            public string Id;
            public string Name;
            public double Cash;
        }

        private class Entry
        {
            public string Id;
            public string Name;
            public object Component;
        }

        private static readonly List<Entry> Dealers = new List<Entry>();

        /// <summary>Dealers recrutados na ultima leitura.</summary>
        public static readonly List<Info> Current = new List<Info>();
        private static PropertyInfo _componentProp; // NPCDealer.Component (interno)
        private static float _nextRefresh;

        /// <summary>Atualiza <see cref="Current"/> com os dealers recrutados e o dinheiro de cada um.</summary>
        public static IReadOnlyList<Info> Read(float now)
        {
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + RefreshListSeconds;
                RefreshList();
            }

            Current.Clear();
            foreach (var dealer in Dealers)
            {
                try
                {
                    if (!(Member(dealer.Component, "IsRecruited") is bool isRecruited) || !isRecruited) continue;
                    var cash = Member(dealer.Component, "Cash") is float c ? c : 0f;
                    Current.Add(new Info { Id = dealer.Id, Name = dealer.Name, Cash = cash });
                }
                catch
                {
                    // dealer destruido entre uma leitura e outra; some no proximo refresh
                }
            }
            return Current;
        }

        public static void Clear()
        {
            Dealers.Clear();
            Current.Clear();
            _nextRefresh = 0;
        }

        private static void RefreshList()
        {
            Dealers.Clear();
            _componentProp ??= typeof(NPCDealer).GetProperty("Component", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (_componentProp == null) return;

            foreach (var npc in NPC.All.ToArray())
            {
                try
                {
                    var wrapper = npc?.Dealer;
                    if (wrapper == null || !wrapper.IsDealer) continue;
                    var component = _componentProp.GetValue(wrapper);
                    if (component == null) continue;
                    var name = npc.FullName;
                    Dealers.Add(new Entry
                    {
                        Id = npc.ID,
                        Name = string.IsNullOrWhiteSpace(name) ? npc.ID : name,
                        Component = component,
                    });
                }
                catch
                {
                    // NPC ainda montando; tenta de novo no proximo refresh
                }
            }
        }

        // Mono expoe campos/propriedades; IL2CPP (Il2CppInterop) expoe tudo como propriedade.
        private static object Member(object target, string name)
        {
            var type = target.GetType();
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return prop.GetValue(target);
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
        }
    }
}
