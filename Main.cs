using HarmonyLib;
using UnityModManagerNet;

namespace DvMod.HingesFix
{
    [EnableReloading]
    public static class Main
    {
        public static UnityModManager.ModEntry? mod;

        static public bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;

            modEntry.OnToggle = OnToggle;

            return true;
        }

        static private bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            if (value)
            {
                harmony.PatchAll();
            }
            else
            {
                harmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }
    }
}
