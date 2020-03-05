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
    public class Controller : Mod
    {
        public Controller(ModContentPack mod) : base(mod)
        {
            HarmonyInstance.Create("pirate_by.FavPriorities").PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"FavPrioritiesMod :: Initialized");
        }
    }
}
