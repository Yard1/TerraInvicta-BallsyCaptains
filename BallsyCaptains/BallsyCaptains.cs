using HarmonyLib;
using UnityModManagerNet;
using PavonisInteractive.TerraInvicta;
using PavonisInteractive.TerraInvicta.Tasks;

namespace BallsyCaptains
{
    // Based on https://github.com/TROYTRON/ti-mods/blob/main/tutorials/code-mods-with-umm.md, thanks Amineri!
    public class BallsyCaptains
    {
        public static bool enabled;
        public static UnityModManager.ModEntry mod;
        public static Settings settings;


        //This is standard code, you can just copy it directly into your mod
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            settings = Settings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            mod = modEntry;
            modEntry.OnToggle = OnToggle;
            FileLog.Log("[BallsyAlienCaptains] Loaded");
            return true;
        }

        //This is also standard code, you can just copy it
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        //Boilerplate code, draws the configurable settings in the UMM
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        //Boilerplate code, saves settings changes to the xml file
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    //Settings class to interface with Unity Mod Manager
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Draw("Alien Evade Chance Multiplier", Collapsible = true)] public float alienEvadeChanceMultiplier = 0.5f;
        [Draw("Human Evade Chance Multiplier", Collapsible = true)] public float humanEvadeChanceMultiplier = 1f;

        //Boilerplate code to save your settings to a Settings.xml file when changed
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        //Hook to allow to do things when a value is changed, if you want
        public void OnChange()
        {
        }
    }

    public enum FactionType
    {
        Human, Alien
    }

    public sealed class State
    {
        private State()
        {
            this.FactionType = FactionType.Human;
        }

        private static State _instance;
        public FactionType FactionType;
        public static State Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new State();
                }
                return _instance;
            }
        }

        public static void Reset()
        {
            State.Instance.FactionType = FactionType.Human;
        }
    }

    [HarmonyPatch(typeof(StratCombatInitStrategy), nameof(StratCombatInitStrategy.SelectStance))]
    public class SelectStancePatch
    {
        static void Prefix(StratCombatInitStrategy __instance, TIFactionState faction, TISpaceCombatState combatState)
        {

            if (BallsyCaptains.enabled)
            {
                if (faction.IsAlienFaction)
                {
                    FileLog.Log($"[BallsyAlienCaptains] faction {faction} is alien faction");
                    State.Instance.FactionType = FactionType.Alien;
                }
                else
                {
                    FileLog.Log($"[BallsyAlienCaptains] faction {faction} is human faction");
                    State.Instance.FactionType = FactionType.Human;
                }
            }
        }
    }

    [HarmonyPatch(typeof(StratCombatInitStrategy), "ChangeStanceWeight")]
    public class ChangeStanceWeightPatch
    {
        static void Prefix(StratCombatInitStrategy __instance, CombatStance stance, ref float value)
        {
            if (BallsyCaptains.enabled && stance == CombatStance.Evade)
            {
                var multiplier = State.Instance.FactionType == FactionType.Alien ? BallsyCaptains.settings.alienEvadeChanceMultiplier : BallsyCaptains.settings.humanEvadeChanceMultiplier;
                if (multiplier >= 0)
                {
                    var newValue = value * multiplier;
                    FileLog.Log($"[BallsyAlienCaptains] ChangeStanceWeight: FactionType {State.Instance.FactionType}, multiplying {value} by {multiplier} to {newValue}");
                    value = newValue;
                }
                State.Reset();
            }
        }
    }
}
