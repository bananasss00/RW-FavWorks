using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FavWorks
{
    [HarmonyPatch(typeof(Selector))]
    [HarmonyPatch("HandleMapClicks")]
    [HarmonyPriority(99999)]
    class FavWorkAdd_MapClick_Patch
    {
        [HarmonyPrefix]
        static bool HandleMapClicks()
        {
            if (Event.current.isKey && Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == FavPrioritiesDefOf.FavWorksThingWorkGivers.MainKey)
            {
                var map = Find.CurrentMap;
                if (map == null)
                    return true;

                if (!Manager.Instance.TryGetFavWorkType(Window.CurrentFavWork, out var cfg))
                {
                    Find.WindowStack.Add(new FloatMenu(
                        new List<FloatMenuOption>
                        {
                            new FloatMenuOption ("UI.CurrentFavWorkNotSelected".Translate(), null)
                        })
                    );
                    return false;
                }

                var mouseCell = UI.MouseCell();
                var colonists = map.mapPawns.FreeColonists.Where(p => !p.Dead).ToList();
                var things = map.thingGrid.ThingsAt(mouseCell).ToList();
                var checkedGivers = new HashSet<WorkGiverDef>();
                var options = new List<FloatMenuOption>();
                foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    foreach (var workGiver in workTypeDef.workGiversByPriority)
                    {
                        if (cfg.ContainsWorkGiver(workGiver))
                            continue;

                        if (workGiver.Worker is WorkGiver_Scanner scanner && scanner.def.directOrderable)
                        { 
                            foreach (var pawn in colonists)
                            {
                                if (checkedGivers.Contains(workGiver))
                                    continue;

                                if (!scanner.ShouldSkip(pawn, true))
                                {
                                    foreach (var thing in things)
                                    {
                                        if (scanner.PotentialWorkThingRequest.Accepts(thing) ||
                                            (scanner.PotentialWorkThingsGlobal(pawn)?.Contains(thing) ?? false))
                                        {
                                            options.Add(new FloatMenuOption(workGiver.LabelCap, () =>
                                            {
                                                cfg.AddWorkGiver(workGiver);
                                                Manager.Instance.ApplyWorks();
                                            }));
                                            checkedGivers.Add(workGiver);
                                            break; // dont iterate next things
                                        }
                                    }

                                    if (scanner.PotentialWorkCellsGlobal(pawn)?.Contains(mouseCell) ?? false)
                                    {
                                        options.Add(new FloatMenuOption(workGiver.LabelCap, () =>
                                        {
                                            cfg.AddWorkGiver(workGiver);
                                            Manager.Instance.ApplyWorks();
                                        }));
                                        checkedGivers.Add(workGiver);
                                        break; // dont iterate next pawns
                                    }
                                }
                            }
                        }
                    }
                }

                if (options.Any())
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                    return false;
                }
            }
            return true;
        }
    }
}