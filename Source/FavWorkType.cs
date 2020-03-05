using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;
using WorkTab;

namespace FavPriorities
{
    /// <summary>
    /// Reset tooltips patch
    /// </summary>
    [HarmonyPatch(typeof(PawnColumnWorker_WorkType), "GetHeaderTip")]
    public static class PawnColumnWorker_WorkType_GetHeaderTip
    {
        [HarmonyPrefix]
        public static void GetHeaderTip(PawnColumnWorker_WorkType __instance, ref string ____headerTip)
        {
            if (Manager.Instance.TryGetFavWorkType(__instance.def.workType, out FavWorkType cfg)
                && cfg.resetTooltipCache)
            {
                ____headerTip = String.Empty;
                cfg.resetTooltipCache = false;
            }
        }
    }

    /// <summary>
    /// Include activated works and store class changes
    /// </summary>
    public class FavWorkType : IExposable
    {
        public FavWorkType()
        {
            
        }
        public FavWorkType(WorkTypeDef workTypeDef) : this()
        {
            this.workTypeDef = workTypeDef;
            this._workTypeName = workTypeDef.defName;
        }

        /// <summary>
        /// Indicate need reset tooltips cache in PawnColumnWorker_WorkType:GetHeaderTip
        /// </summary>
        public bool resetTooltipCache = true;
            
        public WorkTypeDef workTypeDef;

        private bool _classChanged = true;

        public bool IsChanged => _classChanged;

        public void SubmitChanges() => _classChanged = false;

        private string _workTypeName;

        public string WorkTypeName
        {
            get => _workTypeName;
            set
            {
                if (!_workTypeName.Equals(value))
                    _classChanged = true;

                _workTypeName = value;
            }
        }

        private void RenameWorkTypeDef()
        {
            workTypeDef.labelShort = workTypeDef.gerundLabel = workTypeDef.pawnLabel = workTypeDef.verb = _workTypeName;
        }

        private HashSet<WorkGiverDef> _works = new HashSet<WorkGiverDef>();

        public bool ContainsWorkGiver(WorkGiverDef workGiver) => _works.Contains(workGiver);

        public void AddWorkGiver(WorkGiverDef workGiver)
        {
            _classChanged = true;
            _works.Add(workGiver);
        }

        public void RemoveWorkGiver(WorkGiverDef workGiver)
        {
            _classChanged = true;
            _works.Remove(workGiver);
        }

        public void ClearWorkGivers()
        {
            _classChanged = true;
            _works.Clear();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref _workTypeName, "workTypeName");
            Scribe_Defs.Look(ref workTypeDef, "workTypeDef");
            Scribe_Collections.Look(ref _works, "works", LookMode.Def);
        }

        public void ApplyChanges(Dictionary<WorkTypeDef, List<WorkGiverDef>> workgiversByType, List<PawnColumnDef> allColumns)
        {
            if (workgiversByType.ContainsKey(workTypeDef))
            {
                Log.Error($"[FavPriorities] Can't apply changes. workTypeDef exist in dictionary!");
                return;
            }

            var pawnColumnDef = FavWorkExtension.GetFavWorkColumnDef(workTypeDef);
            if (pawnColumnDef == null)
            {
                Log.Error($"[FavPriorities] Can't apply changes. PawnColumnDef = null");
                return;
            }

            // fix expanded column if works count < 2
            if (pawnColumnDef.Worker is IExpandableColumn expandable && expandable.Expanded && _works.Count < 2)
            {
                expandable.Expanded = false;
            }

            // add new column if works > 0
            if (_works.Count > 0)
            {
                // insert before WorkTab Favourite column
                int insertPosition = allColumns.Count - 3;
                allColumns.Insert(insertPosition, pawnColumnDef);
                this.RenameWorkTypeDef();
            }

            workTypeDef.workGiversByPriority = _works.ToList();
            workgiversByType.Add(workTypeDef, workTypeDef.workGiversByPriority.ToList());
            resetTooltipCache = true;
            _classChanged = false;
        }
    }
}