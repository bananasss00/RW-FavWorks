using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTab;

namespace FavWorks
{
    public static class FavWorkExtension
    {
        public const string FavWorkTypeDefBaseName = "FavWork";

        public static bool IsFavWorkDef(this WorkTypeDef def) => def.defName.StartsWith(FavWorkTypeDefBaseName);

        public static bool IsFavWorkColumnDef(this PawnColumnDef def) => 
            def.workerClass == typeof(PawnColumnWorker_WorkType) &&
            def.defName.StartsWith($"WorkPriority_{FavWorkTypeDefBaseName}");

        private static List<PawnColumnDef> _allModPawnColumnDef;

        public static PawnColumnDef GetFavWorkColumnDef(WorkTypeDef def)
        {
            if (!def.IsFavWorkDef())
            {
                Log.Error($"[FavWorks] This is not FavWorkTypeDef!");
                return null;
            }

            if (_allModPawnColumnDef == null)
            {
                _allModPawnColumnDef = DefDatabase<PawnColumnDef>.AllDefs
                    .Where(x => x.IsFavWorkColumnDef())
                    .ToList();
            }

            return _allModPawnColumnDef.FirstOrDefault(x => x.defName.Equals("WorkPriority_" + def.defName));
        }
    }
}