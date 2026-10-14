using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace SmartRestockFix
{
    /// <summary>Acesso, por reflection, as partes internas do SmartRestock (uplusion23, 1.3.x).</summary>
    internal static class SmartRestockBridge
    {
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static MethodInfo _getRuntimeKey;    // RestockManager.GetSlotRuntimeKey(ItemSlot) -> nint
        private static MethodInfo _restockDepleted;  // RestockManager.TryRestockDepletedSlot(ItemSlot, ItemInstance) -> bool
        private static MethodInfo _buildContainerId; // SlotIdentity.TryBuildContainerId(ItemSlot) -> string
        private static MethodInfo _getSlotIndex;     // SlotIdentity.TryGetSlotIndex(ItemSlot) -> int?
        private static MethodInfo _storeGet;         // SmartRestockStateStore.Get(SlotKey) -> bool
        private static MethodInfo _currentSaveSlot;  // SmartRestockStateStore.GetCurrentSaveSlot() -> int
        private static Type _slotKeyType;            // record struct SlotKey(string, int)
        private static IDictionary _enabledSlots;    // ConcurrentDictionary<nint, bool>
        private static IDictionary _runtimeSlotKeys; // ConcurrentDictionary<nint, SlotKey>
        private static IDictionary _runtimeSlots;    // ConcurrentDictionary<nint, ItemSlot>

        public static bool Found { get; private set; }
        public static bool Compatible { get; private set; }

        public static void Resolve()
        {
            var manager = AccessTools.TypeByName("SmartRestock.Logic.RestockManager");
            var identity = AccessTools.TypeByName("SmartRestock.Utils.SlotIdentity");
            var store = AccessTools.TypeByName("SmartRestock.State.SmartRestockStateStore");
            _slotKeyType = AccessTools.TypeByName("SmartRestock.State.SlotKey");
            Found = manager != null && identity != null && store != null && _slotKeyType != null;
            if (!Found) return;

            _getRuntimeKey = manager.GetMethod("GetSlotRuntimeKey", Any);
            _restockDepleted = manager.GetMethod("TryRestockDepletedSlot", Any);
            _buildContainerId = identity.GetMethod("TryBuildContainerId", Any);
            _getSlotIndex = identity.GetMethod("TryGetSlotIndex", Any);
            _storeGet = store.GetMethod("Get", Any);
            _currentSaveSlot = store.GetMethod("GetCurrentSaveSlot", Any);
            _enabledSlots = manager.GetField("EnabledSlots", Any)?.GetValue(null) as IDictionary;
            _runtimeSlotKeys = manager.GetField("RuntimeSlotKeys", Any)?.GetValue(null) as IDictionary;
            _runtimeSlots = manager.GetField("RuntimeSlots", Any)?.GetValue(null) as IDictionary;

            Compatible = _getRuntimeKey != null && _restockDepleted != null && _buildContainerId != null
                         && _getSlotIndex != null && _storeGet != null && _currentSaveSlot != null
                         && _enabledSlots != null && _runtimeSlotKeys != null && _runtimeSlots != null;
        }

        public static int CurrentSaveSlot() => (int)_currentSaveSlot.Invoke(null, null);

        /// <summary>
        /// Garante que o SmartRestock conhece o slot (como se o armario tivesse sido aberto).
        /// Retorna o ID do slot ("Propriedade:Grade:x,y:movel:indice") se o auto-restock
        /// estiver ligado, ou null.
        /// </summary>
        public static string EnsureRegistered(object slot)
        {
            var runtimeKey = _getRuntimeKey.Invoke(null, new[] { slot });
            if (runtimeKey is IntPtr ptr && ptr == IntPtr.Zero) return null;

            // Ja conhecido (aberto nesta sessao ou registrado antes): respeita o que esta la.
            if (_enabledSlots.Contains(runtimeKey))
            {
                if (!(bool)_enabledSlots[runtimeKey]) return null;
                return _runtimeSlotKeys[runtimeKey]?.ToString();
            }

            var key = BuildKey(slot);
            if (key == null) return null;
            var enabled = (bool)_storeGet.Invoke(null, new[] { key });

            _enabledSlots[runtimeKey] = enabled;
            _runtimeSlotKeys[runtimeKey] = key;
            _runtimeSlots[runtimeKey] = slot;
            return enabled ? key.ToString() : null;
        }

        /// <summary>Pede ao SmartRestock para repor o slot vazio com uma copia do item informado.</summary>
        public static bool Restock(object slot, object itemInstance) =>
            (bool)_restockDepleted.Invoke(null, new[] { slot, itemInstance });

        private static object BuildKey(object slot)
        {
            var containerId = _buildContainerId.Invoke(null, new[] { slot }) as string;
            if (string.IsNullOrEmpty(containerId)) return null;
            if (!(_getSlotIndex.Invoke(null, new[] { slot }) is int index) || index < 0) return null;
            return Activator.CreateInstance(_slotKeyType, containerId, index);
        }
    }
}
