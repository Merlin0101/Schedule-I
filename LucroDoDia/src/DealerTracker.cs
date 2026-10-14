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

        private static readonly List<object> Dealers = new List<object>();
        private static PropertyInfo _componentProp; // NPCDealer.Component (interno)
        private static float _nextRefresh;

        /// <summary>Dinheiro total a coletar com os dealers e quantos estao recrutados.</summary>
        public static (double pool, int recruited) Read(float now)
        {
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + RefreshListSeconds;
                RefreshList();
            }

            double pool = 0;
            var recruited = 0;
            foreach (var dealer in Dealers)
            {
                try
                {
                    if (!(Member(dealer, "IsRecruited") is bool isRecruited) || !isRecruited) continue;
                    recruited++;
                    if (Member(dealer, "Cash") is float cash) pool += cash;
                }
                catch
                {
                    // dealer destruido entre uma leitura e outra; some no proximo refresh
                }
            }
            return (pool, recruited);
        }

        public static void Clear()
        {
            Dealers.Clear();
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
                    if (component != null) Dealers.Add(component);
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
