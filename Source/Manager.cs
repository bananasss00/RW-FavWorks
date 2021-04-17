using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab;

namespace FavWorks
{
    [DefOf]
    public static class FavPrioritiesDefOf
    {
        public static KeyBindingDef FavWorksOpenWindow;
        public static KeyBindingDef FavWorksThingWorkGivers;
    }

    /// <summary>
    /// Update columns if WorkTab opened
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), "DoWindowContents")]
    public static class MainTabWindow_WorkTab_DoWindowContents
    {
        [HarmonyPrefix]
        public static void DoWindowContents(ref bool ____columnsChanged)
        {
            if (Manager.Instance.ColumnsUpdated)
            {
                ____columnsChanged = true;
                Manager.Instance.ColumnsUpdated = false;
            }
        }
    }

    public class Manager : GameComponent
    {
        public Manager()
        {
            Instance = this;

            _favWorkTypeDefs = DefDatabase<WorkTypeDef>.AllDefs
                .Where(x => x.IsFavWorkDef())
                .ToDictionary(x => x.defName, y => new FavWorkType(y));

            if (_favWorkTypeDefs.Count == 0)
            {
                Log.Error("[FavWorks] Can't find any FavWork WorkTypeDef's");
            }

            this.ApplyWorks();
        }

        public Manager(Game game) : this()
        {
            this.game = game;
        }

        public override void GameComponentOnGUI()
        {
            if (FavPrioritiesDefOf.FavWorksOpenWindow != null && FavPrioritiesDefOf.FavWorksOpenWindow.IsDownEvent)
            {
                var window = Window.Dialog;
                if (!window.IsOpen)
                {
                    Find.WindowStack.Add(window);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            try
            {
                Scribe_Collections.Look(ref _favWorkTypeDefs, "FavWorkTypeDefs", LookMode.Value, LookMode.Deep );

                foreach (var favWorkTypeDef in _favWorkTypeDefs.Values)
                {
                    if (favWorkTypeDef.WorkTypeDef == null)
                        throw new Exception("workTypeDef was null");
                }

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    _favWorkTypeDefs = _favWorkTypeDefs.ToDictionary(x => x.Value.WorkTypeDef.defName, y => y.Value);
                }
            }
            catch (Exception e)
            {
                _favWorkTypeDefs = DefDatabase<WorkTypeDef>.AllDefs
                    .Where(x => x.IsFavWorkDef())
                    .ToDictionary(x => x.defName, y => new FavWorkType(y));

                Log.Error($"[FavWorks] Exception when loading data: {e.Message}");
            }

            this.ApplyWorks();
        }

        public string GetFavWorkName(WorkTypeDef def)
        {
            if (_favWorkTypeDefs.TryGetValue(def.defName, out FavWorkType favWorkType))
                return favWorkType.WorkTypeName;

            return def.labelShort;
        }

        public bool TryGetFavWorkType(WorkTypeDef def, out FavWorkType favWorkType)
        {
            if (def == null)
            {
                favWorkType = null;
                return false;
            }

            return _favWorkTypeDefs.TryGetValue(def.defName, out favWorkType);
        }

        public bool HasFavWorkTypeChanges() => _favWorkTypeDefs.Values.Any(x => x.IsChanged);

        public List<FavWorkType> GetAllFavWorkTypes() => _favWorkTypeDefs.Values.ToList();

        public void ApplyWorks()
        {
            var workgiversByType = (Dictionary<WorkTypeDef, List<WorkGiverDef>>) WorkgiversByTypeField.GetValue(null);
            var workListCache = (Dictionary<WorkTypeDef, string>) WorkListCacheField.GetValue(null);

            // remove cached WorkTab.WorkType_Extensions:WorkGivers() data for fav worktype
            workgiversByType.RemoveAll(x => x.Key.IsFavWorkDef());
            // remove cached WorkTab.WorkType_Extensions:SpecificWorkListString() data for fav worktype
            workListCache.RemoveAll(x => x.Key.IsFavWorkDef());
            // remove columns fav columns
            WorkTab.Controller.allColumns.RemoveAll(x => x.IsFavWorkColumnDef());

            foreach (var favWorkCfg in _favWorkTypeDefs.Values)
            {
                favWorkCfg.InsertNewWork(workgiversByType, WorkTab.Controller.allColumns);
            }

            // update pawns work settings
            if (Find.Maps != null)
            {
                foreach (var map in Find.Maps)
                {
                    map.mapPawns?.FreeColonists?
                        .ToList()
                        .ForEach(x => x.workSettings.Notify_UseWorkPrioritiesChanged());
                }
            }

            ColumnsUpdated = true;
        }

        public Game game;
        public static Manager Instance;
        public bool ColumnsUpdated { get; set; }
        private Dictionary<string, FavWorkType> _favWorkTypeDefs;

        // WorkTab Fields
        private static readonly FieldInfo WorkListCacheField = AccessTools.Field(typeof(WorkType_Extensions), "workListCache");
        private static readonly FieldInfo WorkgiversByTypeField = AccessTools.Field(typeof(WorkType_Extensions), "_workgiversByType");
    }
}