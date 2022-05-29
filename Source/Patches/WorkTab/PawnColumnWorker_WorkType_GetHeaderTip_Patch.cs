using System;
using HarmonyLib;
using WorkTab;

namespace FavWorks.Patches.WorkTab
{
    /// <summary>
    /// Reset tooltips patch
    /// </summary>
    [HarmonyPatch(typeof(PawnColumnWorker_WorkType), nameof(PawnColumnWorker_WorkType.GetHeaderTip))]
    public static class PawnColumnWorker_WorkType_GetHeaderTip_Patch
    {
        [HarmonyPrefix]
        public static void GetHeaderTip(PawnColumnWorker_WorkType __instance, ref string ____headerTip)
        {
            if (Manager.Instance.TryGetFavWorkType(__instance.def.workType, out var cfg)
                && cfg.ResetTooltipCache)
            {
                ____headerTip = String.Empty;
                cfg.ResetTooltipCache = false;
            }
        }
    }
}