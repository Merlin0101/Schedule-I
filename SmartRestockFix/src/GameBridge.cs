using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SmartRestockFix
{
    /// <summary>
    /// Acesso, por reflection, aos tipos do jogo (build IL2CPP; nomes conferidos no
    /// Assembly-CSharp gerado pelo MelonLoader). Assim o mod compila sem as DLLs do jogo.
    /// </summary>
    internal static class GameBridge
    {
        private static PropertyInfo _slotItem;      // ItemSlot.ItemInstance
        private static PropertyInfo _slotQuantity;  // ItemSlot.Quantity
        private static PropertyInfo _slotOwner;     // ItemSlot.SlotOwner
        private static MethodInfo _getItem;         // Registry.GetItem(string) -> ItemDefinition
        private static PropertyInfo _loadInstance;  // Singleton<LoadManager>.Instance
        private static PropertyInfo _isGameLoaded;  // LoadManager.IsGameLoaded
        private static PropertyInfo _isLoading;     // LoadManager.IsLoading

        public static Type ItemSlotType { get; private set; }

        public static bool Resolve()
        {
            ItemSlotType = AccessTools.TypeByName("Il2CppScheduleOne.ItemFramework.ItemSlot");
            var registry = AccessTools.TypeByName("Il2CppScheduleOne.Registry");
            var loadManager = AccessTools.TypeByName("Il2CppScheduleOne.Persistence.LoadManager");
            if (ItemSlotType == null || registry == null || loadManager == null) return false;

            _slotItem = ItemSlotType.GetProperty("ItemInstance");
            _slotQuantity = ItemSlotType.GetProperty("Quantity");
            _slotOwner = ItemSlotType.GetProperty("SlotOwner");
            _getItem = registry.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m =>
                m.Name == "GetItem" && !m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
            _loadInstance = loadManager.GetProperty("Instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            _isGameLoaded = loadManager.GetProperty("IsGameLoaded");
            _isLoading = loadManager.GetProperty("IsLoading");

            return _slotItem != null && _slotQuantity != null && _slotOwner != null && _getItem != null
                   && _loadInstance != null && _isGameLoaded != null && _isLoading != null;
        }

        public static bool IsGameReady()
        {
            var manager = _loadInstance.GetValue(null);
            return manager != null && (bool)_isGameLoaded.GetValue(manager) && !(bool)_isLoading.GetValue(manager);
        }

        public static bool IsAlive(object slot)
        {
            try { return _slotOwner.GetValue(slot) != null; }
            catch { return false; }
        }

        public static int Quantity(object slot) => (int)_slotQuantity.GetValue(slot);

        /// <summary>ID do item no slot (ex: "ogkushseed"), ou null se vazio.</summary>
        public static string ItemId(object slot)
        {
            var item = _slotItem.GetValue(slot);
            return item?.GetType().GetProperty("ID")?.GetValue(item) as string;
        }

        /// <summary>Cria 1 unidade do item pelo ID, ou null se o ID nao existir mais.</summary>
        public static object CreateItem(string itemId)
        {
            var definition = _getItem.Invoke(null, new object[] { itemId });
            if (definition == null) return null;
            var create = definition.GetType().GetMethod("GetDefaultInstance", new[] { typeof(int) });
            return create?.Invoke(definition, new object[] { 1 });
        }
    }
}
