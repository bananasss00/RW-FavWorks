using System;
using System.Reflection;
using Harmony;
using Verse;
using System.Linq;
using System.Collections.Generic;
using HugsLib;
using RimWorld;
using UnityEngine;
using WorkTab;

namespace FavPriorities
{
    // TODO: multi groups(hide if empty), save load groups
    public class Controller : ModBase
    {
        //WorkTab.Controller.allColumns.RemoveAll(x => x.defName.Contains("FavPriority1")); hide / show columns

        public override string ModIdentifier => "FavPriorities";

        public override void DefsLoaded()
        {
            HarmonyInstance.Create("rimworld.harmony.FavPriorities").PatchAll(Assembly.GetExecutingAssembly());

            FavDef1 = DefDatabase<WorkTypeDef>.AllDefs.FirstOrDefault(x => x.defName == "FavPriority1");
            FavDef2 = DefDatabase<WorkTypeDef>.AllDefs.FirstOrDefault(x => x.defName == "FavPriority2");
            FavDef3 = DefDatabase<WorkTypeDef>.AllDefs.FirstOrDefault(x => x.defName == "FavPriority3");

            AllWorkTypes = DefDatabase<WorkTypeDef>.AllDefs
                .Where(x => !x.defName.StartsWith("FavPriority"))
                .ToList();

            Log.Message($"FavPrioritiesMod :: Initialized");
        }

        public static void ApplyWorks()
        {
            FavDef1.workGiversByPriority = FavWorks.ToList();

            var workgiversByType = (Dictionary<WorkTypeDef, List<WorkGiverDef>>)WorkgiversByTypeField.GetValue(null);
            if (!workgiversByType.TryGetValue(FavDef1, out List<WorkGiverDef> result)){}
            workgiversByType[FavDef1] = new List<WorkGiverDef>(FavDef1.workGiversByPriority);

            ResetTooltipCaches();
            ResetPawns();
        }

        public static void ResetTooltipCaches()
        {
            if (WorkListCacheField == null)
                throw new Exception("workListCache == null");

            ((Dictionary<WorkTypeDef, string>)WorkListCacheField.GetValue(null)).Clear();
            PawnColumnWorker_WorkType_GetHeaderTip.NeedResetCache = true;
        }

        public static void ResetPawns()
        {
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
                pawn.workSettings.Notify_UseWorkPrioritiesChanged();
        }

        public static List<WorkTypeDef> AllWorkTypes = new List<WorkTypeDef>();
        public static HashSet<WorkGiverDef> FavWorks = new HashSet<WorkGiverDef>();
        public static WorkTypeDef FavDef1, FavDef2, FavDef3;
        private static readonly FieldInfo WorkListCacheField = AccessTools.Field(typeof(WorkType_Extensions), "workListCache");
        private static readonly FieldInfo WorkgiversByTypeField = AccessTools.Field(typeof(WorkType_Extensions), "_workgiversByType");
    }

    [HarmonyPatch(typeof(PawnColumnWorker_WorkType), "GetHeaderTip")]
    public static class PawnColumnWorker_WorkType_GetHeaderTip
    {
        public static bool NeedResetCache { get; set; } = true;

        [HarmonyPrefix]
        public static void GetHeaderTip(PawnColumnWorker_WorkType __instance, ref string ____headerTip)
        {
            if (NeedResetCache && __instance.def.defName == "WorkPriority_FavPriority1")
            {
                ____headerTip = "";
                NeedResetCache = false;
            }
        }
    }
}
