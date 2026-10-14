using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(SmartRestockFix.Core), SmartRestockFix.Core.ModName, SmartRestockFix.Core.ModVersion, "Fun-on-rio")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SmartRestockFix
{
    /// <summary>
    /// Complemento do SmartRestock (uplusion23, 1.3.x):
    ///
    /// 1. Armarios nao abertos na sessao: o SmartRestock so conhece um slot depois que o
    ///    botao dele aparece na tela. Rodamos antes dele em ItemSlot.ChangeQuantity e
    ///    registramos o slot a partir do estado salvo (slot-states-N.json).
    /// 2. Memoria de produto: anotamos o item de cada slot ligado (slot-items-N.json).
    ///    Depois de carregar o save, slots ligados que ficaram vazios sao repostos com
    ///    esse item pelo proprio SmartRestock (preco, filtros, notificacao e dinheiro dele).
    /// </summary>
    public class Core : MelonMod
    {
        public const string ModName = "SmartRestockFix";
        public const string ModVersion = "1.1.0";

        private const float RestoreDelaySeconds = 5f; // espera o mundo terminar de montar apos o load
        private const float FlushIntervalSeconds = 2f;

        private static MelonLogger.Instance _log;
        private static SlotMemory _memory;
        private static readonly List<object> KnownSlots = new List<object>(); // slots criados nesta cena
        private static bool _warnedFailure;

        private static MelonPreferences_Entry<bool> _restoreEmpty;
        private bool _enabled;
        private bool _restoreDone;
        private float _readySince = -1f;
        private float _nextFlush;

        private static float Now => Environment.TickCount64 / 1000f;

        public override void OnInitializeMelon()
        {
            _log = LoggerInstance;
            var category = MelonPreferences.CreateCategory(ModName);
            _restoreEmpty = category.CreateEntry("ReporSlotsVaziosAoCarregar", true,
                description: "Ao carregar o save, repoe slots com auto-restock ligado que ficaram vazios, usando o ultimo produto conhecido.");
            _memory = new SlotMemory(Path.Combine(MelonEnvironment.UserDataDirectory, ModName));
            TryPatch();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!_enabled && !SmartRestockBridge.Found) TryPatch(); // SmartRestock carregou depois?
            Flush();
            if (sceneName != "Main") KnownSlots.Clear(); // voltou ao menu: slots do save anterior
            _restoreDone = false;
            _readySince = -1f;
        }

        public override void OnApplicationQuit() => Flush();

        private void TryPatch()
        {
            SmartRestockBridge.Resolve();
            if (!SmartRestockBridge.Found)
            {
                LoggerInstance.Warning("SmartRestock nao encontrado (instalado?). Nada a corrigir por enquanto.");
                return;
            }
            if (!SmartRestockBridge.Compatible || !GameBridge.Resolve())
            {
                LoggerInstance.Warning("Esta versao do SmartRestock ou do jogo mudou por dentro; complemento desativado para nao causar problemas.");
                return;
            }

            var changeQuantity = AccessTools.Method(GameBridge.ItemSlotType, "ChangeQuantity");
            var setSlotOwner = AccessTools.Method(GameBridge.ItemSlotType, "SetSlotOwner");
            if (changeQuantity == null || setSlotOwner == null)
            {
                LoggerInstance.Warning("Metodos do ItemSlot nao encontrados; complemento desativado.");
                return;
            }

            // Priority.First: registra o slot antes do prefix do SmartRestock decidir se repoe.
            HarmonyInstance.Patch(changeQuantity, prefix: new HarmonyMethod(typeof(Core), nameof(ChangeQuantityPrefix))
            {
                priority = HarmonyLib.Priority.First
            });
            HarmonyInstance.Patch(setSlotOwner, postfix: new HarmonyMethod(typeof(Core), nameof(SetSlotOwnerPostfix)));
            _enabled = true;
            LoggerInstance.Msg("Correcao aplicada: armarios repoem sem abrir e slots vazios sao lembrados entre sessoes.");
        }

        public override void OnUpdate()
        {
            if (!_enabled) return;

            if (Now >= _nextFlush)
            {
                _nextFlush = Now + FlushIntervalSeconds;
                Flush();
            }

            if (_restoreDone) return;
            bool ready;
            try { ready = GameBridge.IsGameReady(); }
            catch { ready = false; }
            if (!ready)
            {
                _readySince = -1f;
                return;
            }
            if (_readySince < 0) _readySince = Now;
            if (Now - _readySince < RestoreDelaySeconds) return;

            _restoreDone = true;
            if (_restoreEmpty.Value) RestoreEmptySlots();
        }

        // ---------- patches ----------

        private static void SetSlotOwnerPostfix(object __instance)
        {
            if (__instance != null) KnownSlots.Add(__instance);
        }

        private static void ChangeQuantityPrefix(object __instance, int change)
        {
            if (__instance == null || change == 0) return;
            try
            {
                var key = SmartRestockBridge.EnsureRegistered(__instance);
                if (key == null) return;
                SyncSaveSlot();
                _memory.Remember(key, GameBridge.ItemId(__instance)); // antes da mudanca: ainda tem o item
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        // ---------- reposicao apos carregar ----------

        private static void RestoreEmptySlots()
        {
            int restocked = 0, unknown = 0, learned = 0;
            try
            {
                SyncSaveSlot();
                KnownSlots.RemoveAll(slot => !GameBridge.IsAlive(slot));
                foreach (var slot in KnownSlots.ToArray())
                {
                    var key = SmartRestockBridge.EnsureRegistered(slot);
                    if (key == null) continue; // sem auto-restock ligado

                    if (GameBridge.Quantity(slot) > 0)
                    {
                        // Slot cheio: ja anota o produto para a proxima vez.
                        if (_memory.Remember(key, GameBridge.ItemId(slot))) learned++;
                        continue;
                    }

                    var itemId = _memory.Recall(key);
                    var item = itemId != null ? GameBridge.CreateItem(itemId) : null;
                    if (item == null)
                    {
                        unknown++;
                        continue;
                    }
                    if (SmartRestockBridge.Restock(slot, item)) restocked++;
                }
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }

            _log.Msg($"Apos carregar ({KnownSlots.Count} slots vistos): {restocked} slot(s) vazio(s) reposto(s), {learned} produto(s) novo(s) memorizado(s)" +
                     (unknown > 0 ? $", {unknown} sem produto conhecido (coloque 1 unidade neles uma vez)." : "."));
        }

        // ---------- utilitarios ----------

        private static void SyncSaveSlot()
        {
            _memory.UseSaveSlot(SmartRestockBridge.CurrentSaveSlot());
        }

        private static void Flush()
        {
            try { _memory?.Flush(); }
            catch (Exception e) { WarnOnce(e); }
        }

        private static void WarnOnce(Exception e)
        {
            if (_warnedFailure) return;
            _warnedFailure = true;
            _log.Warning($"Falha no complemento ({(e.InnerException ?? e).Message}). O SmartRestock segue funcionando normalmente.");
        }
    }
}
