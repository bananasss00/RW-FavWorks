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
            FavWorkTypeDefs = DefDatabase<WorkTypeDef>.AllDefs
                .Where(x => x.defName.StartsWith("FavWork"))
                .ToDictionary(x => x.defName, y => new FavWorkCfg(y));

            if (FavWorkTypeDefs.Count == 0)
            {
                Log.Error("[FavPriorities] Can't find any FavWork WorkTypeDef's");
                return;
            }

            AllModPawnColumnDef = DefDatabase<PawnColumnDef>.AllDefs
                .Where(x => x.workerClass == typeof(PawnColumnWorker_WorkType)
                            && x.defName.StartsWith("WorkPriority_FavWork"))
                .ToList();

            if (AllModPawnColumnDef.Count == 0)
            {
                Log.Error("[FavPriorities] Can't find any FavWork PawnColumnDef's");
                return;
            }
            if (AllModPawnColumnDef.Count != FavWorkTypeDefs.Count)
            {
                Log.Error("[FavPriorities] PawnColumnDef's != WorkTypeDef's");
                return;
            }

            AllWorkTypes = DefDatabase<WorkTypeDef>.AllDefs
                .Where(x => !x.defName.StartsWith("FavWork"))
                .ToList();

            ApplyWorks();

            HarmonyInstance.Create("rimworld.harmony.FavPriorities").PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"FavPrioritiesMod :: Initialized");
        }

        public static void ApplyWorks()
        {
            var workgiversByType = (Dictionary<WorkTypeDef, List<WorkGiverDef>>)WorkgiversByTypeField.GetValue(null);

            // clean olds
            workgiversByType.RemoveAll(x => x.Key.defName.StartsWith("FavWork"));

            foreach (var kv in FavWorkTypeDefs)
            {
                var favWorkType = kv.Value.favWorkType;
                var favWorkCfg = kv.Value;
                favWorkType.workGiversByPriority = favWorkCfg.FavWorks.ToList();

                HideEmptyColumns(favWorkType, favWorkCfg);
                workgiversByType.Add(favWorkType, new List<WorkGiverDef>(favWorkType.workGiversByPriority));
                favWorkCfg.needUpdate = true; 
            }

            FixExpandedColumns();
            ResetTooltipCaches();
            ResetPawns();
        }

        private static void FixExpandedColumns()
        {
            foreach (var pawnColumnDef in WorkTab.Controller.allColumns.Where(x => x.defName.StartsWith("WorkPriority_FavWork")))
            {
                var expandable = pawnColumnDef.Worker as IExpandableColumn;
                if (expandable != null && expandable.Expanded && FavWorkTypeDefs[pawnColumnDef.workType.defName].FavWorks.Count < 2)
                {
                    expandable.Expanded = false;
                }
            }
        }

        // change GetHashCode for WorkTypeDef! workTypeDef.labelShort = workTypeDef.gerundLabel = workTypeDef.pawnLabel = cfg.name;
        private static void HideEmptyColumns(WorkTypeDef workTypeDef, FavWorkCfg cfg)
        {
            if (cfg.FavWorks.Count == 0)
            {
                WorkTab.Controller.allColumns.RemoveAll(x => x.defName.Equals("WorkPriority_" + workTypeDef.defName));
            }
            else if (!WorkTab.Controller.allColumns.Exists(x => x.defName.Equals("WorkPriority_" + workTypeDef.defName)))
            {
                var pawnColumn = AllModPawnColumnDef.FirstOrDefault(x => x.defName.Equals("WorkPriority_" + workTypeDef.defName));
                if (pawnColumn != null)
                {
                    // insert before WorkTab Favourite column
                    var position = WorkTab.Controller.allColumns.Count - 3;
                    WorkTab.Controller.allColumns.Insert(position, pawnColumn);
                    workTypeDef.labelShort = workTypeDef.gerundLabel = workTypeDef.pawnLabel = cfg.name;
                }
            }
        }

        private static void ResetTooltipCaches()
        {
            if (WorkListCacheField == null)
                throw new Exception("workListCache == null");

            ((Dictionary<WorkTypeDef, string>)WorkListCacheField.GetValue(null)).Clear();
        }

        private static void ResetPawns()
        {
            var maps = Find.Maps;
            if (maps == null)
                return;

            foreach (var map in maps)
            {
                map.mapPawns?.FreeColonists?.ToList().ForEach(x => x.workSettings.Notify_UseWorkPrioritiesChanged());
            }
        }

        public class FavWorkCfg : IExposable
        {
            public FavWorkCfg()
            {
                
            }
            public FavWorkCfg(WorkTypeDef wotkType)
            {
                favWorkType = wotkType;
            }

            public WorkTypeDef favWorkType;
            public HashSet<WorkGiverDef> FavWorks = new HashSet<WorkGiverDef>();
            public string name = "FavWorks";
            public bool needUpdate = true;

            public void ExposeData()
            {
                Scribe_Values.Look(ref name, "name");
                Scribe_Defs.Look(ref favWorkType, "favWorkType");
                Scribe_Collections.Look(ref FavWorks, "FavWorks", LookMode.Def);
            }
        }

        public static List<PawnColumnDef> AllModPawnColumnDef;
        public static List<WorkTypeDef> AllWorkTypes = new List<WorkTypeDef>();
        public static Dictionary<string, FavWorkCfg> FavWorkTypeDefs;
        // worktab fields
        private static readonly FieldInfo WorkListCacheField = AccessTools.Field(typeof(WorkType_Extensions), "workListCache");
        private static readonly FieldInfo WorkgiversByTypeField = AccessTools.Field(typeof(WorkType_Extensions), "_workgiversByType");
    }

    [HarmonyPatch(typeof(PawnColumnWorker_WorkType), "GetHeaderTip")]
    public static class PawnColumnWorker_WorkType_GetHeaderTip
    {
        [HarmonyPrefix]
        public static void GetHeaderTip(PawnColumnWorker_WorkType __instance, ref string ____headerTip)
        {
            if (Controller.FavWorkTypeDefs.TryGetValue(__instance.def.workType.defName, out Controller.FavWorkCfg cfg)
                && cfg.needUpdate)
            {
                ____headerTip = "";
                cfg.needUpdate = false;
            }
        }
    }
}
