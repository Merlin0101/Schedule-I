using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace GrowFaster
{
    /// <summary>
    /// Encurta o tempo de crescimento de cada planta quando ela e inicializada.
    ///
    /// O patch e aplicado por reflection para funcionar nas duas builds do jogo:
    ///   - Mono:   ScheduleOne.Growing.Plant
    ///   - IL2CPP: Il2CppScheduleOne.Growing.Plant (gerado pelo Il2CppInterop)
    /// Se uma atualizacao do jogo renomear algo, o mod so registra um aviso
    /// no console em vez de quebrar o jogo.
    /// </summary>
    internal static class PlantPatches
    {
        private static readonly string[] PlantTypeNames =
        {
            "ScheduleOne.Growing.Plant",
            "Il2CppScheduleOne.Growing.Plant",
        };

        private const string InitializeMethod = "Initialize";
        private const string GrowthTimeMember = "GrowthTime";

        private static MelonLogger.Instance _log;
        private static FieldInfo _growthField;
        private static PropertyInfo _growthProperty;

        public static void Apply(HarmonyLib.Harmony harmony, MelonLogger.Instance log)
        {
            _log = log;

            Type plantType = null;
            foreach (var name in PlantTypeNames)
            {
                plantType = AccessTools.TypeByName(name);
                if (plantType != null) break;
            }
            if (plantType == null)
            {
                log.Warning("Tipo Plant nao encontrado - o jogo pode ter mudado. Mod desativado.");
                return;
            }

            // Mono expoe GrowthTime como campo; o Il2CppInterop transforma campos em propriedades.
            _growthField = AccessTools.Field(plantType, GrowthTimeMember);
            if (_growthField == null)
                _growthProperty = AccessTools.Property(plantType, GrowthTimeMember);
            if (_growthField == null && _growthProperty == null)
            {
                log.Warning($"{plantType.FullName}.{GrowthTimeMember} nao encontrado. Mod desativado.");
                return;
            }

            var target = AccessTools.Method(plantType, InitializeMethod);
            if (target == null)
            {
                log.Warning($"{plantType.FullName}.{InitializeMethod} nao encontrado. Mod desativado.");
                return;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(typeof(PlantPatches), nameof(InitializePostfix)));
            log.Msg($"Patch aplicado em {plantType.FullName}.{InitializeMethod}");
        }

        private static void InitializePostfix(object __instance)
        {
            if (!Core.Enabled) return;

            try
            {
                var current = Convert.ToDouble(GetGrowthTime(__instance));
                var scaled = Math.Max(1, (int)Math.Round(current / Core.GrowthMultiplier));
                SetGrowthTime(__instance, scaled);
            }
            catch (Exception e)
            {
                _log.Error($"Falha ao ajustar GrowthTime: {e.Message}");
            }
        }

        private static object GetGrowthTime(object plant) =>
            _growthField != null ? _growthField.GetValue(plant) : _growthProperty.GetValue(plant);

        private static void SetGrowthTime(object plant, int value)
        {
            // Respeita o tipo original do membro (int ou float).
            var memberType = _growthField != null ? _growthField.FieldType : _growthProperty.PropertyType;
            var boxed = Convert.ChangeType(value, memberType);
            if (_growthField != null) _growthField.SetValue(plant, boxed);
            else _growthProperty.SetValue(plant, boxed);
        }
    }
}
