using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTab;

namespace FavWorks
{
    /// <summary>
    /// Dynamic WorkTypeDef and track class changes
    /// </summary>
    public class FavWorkType : IExposable
    {
        /// <summary>
        /// Used for exposing data
        /// </summary>
        public FavWorkType()
        {
            
        }

        public FavWorkType(WorkTypeDef workTypeDef) : this()
        {
            WorkTypeDef = workTypeDef;
            _workTypeName = workTypeDef.defName;
            WorkTypeDef.description = string.Empty;
        }

        /// <summary>
        /// Indicate need reset tooltips cache in PawnColumnWorker_WorkType:GetHeaderTip
        /// </summary>
        public bool ResetTooltipCache = true;
            
        public WorkTypeDef WorkTypeDef;

        public bool IsChanged { get; private set; } = true;

        private string _workTypeName = "undefined";

        public string WorkTypeName
        {
            get => _workTypeName;
            set
            {
                if (!_workTypeName.Equals(value))
                    IsChanged = true;

                _workTypeName = value;
            }
        }

        private HashSet<WorkGiverDef> _works = new();

        public bool ContainsWorkGiver(WorkGiverDef workGiver) => _works.Contains(workGiver);

        public void AddWorkGiver(WorkGiverDef workGiver)
        {
            IsChanged = true;
            _works.Add(workGiver);
        }

        public void RemoveWorkGiver(WorkGiverDef workGiver)
        {
            IsChanged = true;
            _works.Remove(workGiver);
        }

        public void ClearWorkGivers()
        {
            IsChanged = true;
            _works.Clear();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref _workTypeName!, "workTypeName");
            Scribe_Defs.Look(ref WorkTypeDef, "workTypeDef");
            Scribe_Collections.Look(ref _works, "works", LookMode.Def);
        }

        public void InsertNewWork(Dictionary<WorkTypeDef, List<WorkGiverDef>> workgiversByType, List<PawnColumnDef> allColumns)
        {
            if (workgiversByType.ContainsKey(WorkTypeDef))
            {
                Log.Error($"[FavWorks] Can't apply changes. workTypeDef exist in dictionary!");
                return;
            }

            var pawnColumnDef = WorkTypeDef.GetFavWorkColumnDef();
            if (pawnColumnDef == null)
            {
                Log.Error($"[FavWorks] Can't apply changes. PawnColumnDef = null");
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
                WorkTypeDef.labelShort = WorkTypeDef.gerundLabel = WorkTypeDef.pawnLabel = WorkTypeDef.verb = _workTypeName;
            }

            WorkTypeDef.workGiversByPriority = _works.ToList();
            workgiversByType.Add(WorkTypeDef, WorkTypeDef.workGiversByPriority.ToList());
            ResetTooltipCache = true;
            IsChanged = false;
        }
    }
}