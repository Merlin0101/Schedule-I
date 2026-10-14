using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

[assembly: MelonInfo(typeof(SmartRestockFix.Core), SmartRestockFix.Core.ModName, SmartRestockFix.Core.ModVersion, "Fun-on-rio")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SmartRestockFix
{
    /// <summary>
    /// Corrige o SmartRestock (1.3.x) nao repor armarios que nao foram abertos na sessao.
    ///
    /// O SmartRestock salva os slots ligados em UserData/SmartRestock/slot-states-N.json,
    /// mas na hora de repor so consulta uma lista em memoria (EnabledSlots) que e
    /// preenchida quando o botao do slot aparece na tela. Este mod roda antes do
    /// SmartRestock em ItemSlot.ChangeQuantity (o mesmo gatilho que ele usa): se o
    /// slot ainda nao esta na lista, calcula o mesmo ID que o SmartRestock usa,
    /// le o estado salvo e registra o slot - como se o armario tivesse sido aberto.
    /// </summary>
    public class Core : MelonMod
    {
        public const string ModName = "SmartRestockFix";
        public const string ModVersion = "1.0.0";

        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static MelonLogger.Instance _log;
        private static bool _patched;
        private static bool _warnedFailure;
        private static readonly HashSet<string> Announced = new HashSet<string>();

        // Membros internos do SmartRestock, achados por reflection.
        private static MethodInfo _getRuntimeKey;    // RestockManager.GetSlotRuntimeKey(ItemSlot) -> nint
        private static MethodInfo _buildContainerId; // SlotIdentity.TryBuildContainerId(ItemSlot) -> string
        private static MethodInfo _getSlotIndex;     // SlotIdentity.TryGetSlotIndex(ItemSlot) -> int?
        private static MethodInfo _storeGet;         // SmartRestockStateStore.Get(SlotKey) -> bool
        private static Type _slotKeyType;            // record struct SlotKey(string, int)
        private static IDictionary _enabledSlots;    // ConcurrentDictionary<nint, bool>
        private static IDictionary _runtimeSlotKeys; // ConcurrentDictionary<nint, SlotKey>
        private static IDictionary _runtimeSlots;    // ConcurrentDictionary<nint, ItemSlot>

        public override void OnInitializeMelon()
        {
            _log = LoggerInstance;
            TryPatch();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Caso o SmartRestock tenha carregado depois deste mod.
            if (!_patched) TryPatch();
            if (sceneName != "Main") Announced.Clear();
        }

        private void TryPatch()
        {
            var manager = AccessTools.TypeByName("SmartRestock.Logic.RestockManager");
            var identity = AccessTools.TypeByName("SmartRestock.Utils.SlotIdentity");
            var store = AccessTools.TypeByName("SmartRestock.State.SmartRestockStateStore");
            _slotKeyType = AccessTools.TypeByName("SmartRestock.State.SlotKey");
            if (manager == null || identity == null || store == null || _slotKeyType == null)
            {
                LoggerInstance.Warning("SmartRestock nao encontrado (instalado?). Nada a corrigir por enquanto.");
                return;
            }

            var changeQuantity = FindChangeQuantity();
            _getRuntimeKey = manager.GetMethod("GetSlotRuntimeKey", Any);
            _buildContainerId = identity.GetMethod("TryBuildContainerId", Any);
            _getSlotIndex = identity.GetMethod("TryGetSlotIndex", Any);
            _storeGet = store.GetMethod("Get", Any);
            _enabledSlots = manager.GetField("EnabledSlots", Any)?.GetValue(null) as IDictionary;
            _runtimeSlotKeys = manager.GetField("RuntimeSlotKeys", Any)?.GetValue(null) as IDictionary;
            _runtimeSlots = manager.GetField("RuntimeSlots", Any)?.GetValue(null) as IDictionary;

            if (changeQuantity == null || _getRuntimeKey == null || _buildContainerId == null || _getSlotIndex == null
                || _storeGet == null || _enabledSlots == null || _runtimeSlotKeys == null || _runtimeSlots == null)
            {
                LoggerInstance.Warning("Esta versao do SmartRestock mudou por dentro; correcao desativada para nao causar problemas.");
                _patched = true; // nao tenta de novo a cada cena
                return;
            }

            // Priority.First: registra o slot antes do prefix do SmartRestock decidir se repoe.
            var prefix = new HarmonyMethod(typeof(Core).GetMethod(nameof(ChangeQuantityPrefix), Any))
            {
                priority = HarmonyLib.Priority.First
            };
            HarmonyInstance.Patch(changeQuantity, prefix: prefix);
            _patched = true;
            LoggerInstance.Msg("Correcao aplicada: armarios configurados repoem mesmo sem abrir.");
        }

        private static MethodInfo FindChangeQuantity()
        {
            foreach (var name in new[] { "Il2CppScheduleOne.ItemFramework.ItemSlot", "ScheduleOne.ItemFramework.ItemSlot" })
            {
                var type = AccessTools.TypeByName(name);
                if (type != null) return AccessTools.Method(type, "ChangeQuantity");
            }
            return null;
        }

        private static void ChangeQuantityPrefix(object __instance, int change)
        {
            // So interessa quando sai item do slot (e quando o SmartRestock pode repor).
            if (change < 0 && __instance != null)
                EnsureRegistered(__instance);
        }

        private static void EnsureRegistered(object slot)
        {
            try
            {
                var runtimeKey = _getRuntimeKey.Invoke(null, new[] { slot });
                if (runtimeKey is IntPtr ptr && ptr == IntPtr.Zero) return;

                // Ja conhecido pelo SmartRestock (aberto nesta sessao): respeita o que ele decidiu.
                if (_enabledSlots.Contains(runtimeKey)) return;

                var containerId = _buildContainerId.Invoke(null, new[] { slot }) as string;
                if (string.IsNullOrEmpty(containerId)) return;
                var index = _getSlotIndex.Invoke(null, new[] { slot }) as int?;
                if (index is not int slotIndex || slotIndex < 0) return;

                var key = Activator.CreateInstance(_slotKeyType, containerId, slotIndex);
                var enabled = (bool)_storeGet.Invoke(null, new[] { key });

                // Registra igual o SmartRestock faria ao abrir o armario.
                _enabledSlots[runtimeKey] = enabled;
                _runtimeSlotKeys[runtimeKey] = key;
                _runtimeSlots[runtimeKey] = slot;

                if (enabled && Announced.Add(key.ToString()))
                    _log.Msg($"Slot restaurado sem abrir o armario: {key}");
            }
            catch (Exception e)
            {
                if (_warnedFailure) return;
                _warnedFailure = true;
                _log.Warning($"Nao foi possivel verificar um slot ({(e.InnerException ?? e).Message}). O SmartRestock segue funcionando normalmente.");
            }
        }
    }
}
