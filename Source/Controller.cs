using System.Reflection;
using HarmonyLib;
using Verse;

namespace FavWorks
{
    public class Controller : Mod
    {
        public Controller(ModContentPack mod) : base(mod)
        {
            new Harmony("PirateBY.FavWorks").PatchAll();
            Log.Message($"FavWorks :: Initialized");
        }
    }
}
