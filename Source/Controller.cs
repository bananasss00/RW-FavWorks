using System.Reflection;
using Harmony;
using Verse;

namespace FavWorks
{
    public class Controller : Mod
    {
        public Controller(ModContentPack mod) : base(mod)
        {
            HarmonyInstance.Create("PirateBY.FavWorks").PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"FavWorks :: Initialized");
        }
    }
}
