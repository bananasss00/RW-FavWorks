using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace FavWorks.Patches.AutoPriorities
{
    /// <summary>
    /// Remove FavWork worktypes from this mod
    /// </summary>
    [HarmonyPatch]
    public class AutoPrioritiesSupport_Patch
    {
        static bool Prepare() => ModLister.GetActiveModWithIdentifier("LokiVKlokeNaAndoke.AutoPriorities") != null;
        static MethodBase TargetMethod() => AccessTools.Method("AutoPriorities.PawnsData:Rebuild");

        static IEnumerable<WorkTypeDef> WorkTypeDefsInPriorityOrder() => WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder.Where(x => !x.defName.StartsWith("FavWork"));

        [HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Rebuild(IEnumerable<CodeInstruction> instructions)
        {
			return instructions.MethodReplacer(
                AccessTools.Method("Verse.WorkTypeDefsUtility:get_WorkTypeDefsInPriorityOrder"),
                AccessTools.Method(typeof(AutoPrioritiesSupport_Patch), nameof(WorkTypeDefsInPriorityOrder)));
        }
    }
}