using MelonLoader;

[assembly: MelonInfo(typeof(GrowFaster.Core), GrowFaster.Core.ModName, GrowFaster.Core.ModVersion, "Fun-on-rio")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace GrowFaster
{
    public class Core : MelonMod
    {
        public const string ModName = "GrowFaster";
        public const string ModVersion = "1.0.0";

        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<float> _growthMultiplier;

        public static bool Enabled => _enabled.Value;

        // Clampado para evitar divisao por zero ou valores absurdos vindos do .cfg.
        public static float GrowthMultiplier
        {
            get
            {
                var value = _growthMultiplier.Value;
                if (value < 0.1f) return 0.1f;
                if (value > 100f) return 100f;
                return value;
            }
        }

        public override void OnInitializeMelon()
        {
            // Gera/atualiza UserData/MelonPreferences.cfg com a secao [GrowFaster].
            _category = MelonPreferences.CreateCategory(ModName);
            _enabled = _category.CreateEntry("Enabled", true,
                description: "Liga/desliga o mod sem precisar remover a DLL.");
            _growthMultiplier = _category.CreateEntry("GrowthMultiplier", 2f,
                description: "2 = plantas crescem 2x mais rapido; 0.5 = 2x mais devagar. Faixa: 0.1 a 100.");

            PlantPatches.Apply(HarmonyInstance, LoggerInstance);

            LoggerInstance.Msg($"Carregado. Multiplicador de crescimento: {GrowthMultiplier}x (ativo: {Enabled})");
        }
    }
}
