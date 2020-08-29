using AutoPatcher.Utility;
using HarmonyLib;
using HugsLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AutoPatcher
{
    internal class Controller : ModBase
    {
        public override void DefsLoaded()
        {
            // Run patch trees
            // bool fromSave = true;
            bool fromSave = false;
            /*var settings = MainMod.thisMod.settings;
            var currMods = ModsConfig.ActiveModsInLoadOrder.Select(t => t.PackageId);
            var savedMods = settings.modList;
            if (savedMods.EnumerableNullOrEmpty() ||  savedMods.SetEquals(currMods))
                fromSave = false;*/
            var patchTrees = DefDatabase<PatchTreeDef>.AllDefs;
            NodeUtility.allNodeDefs.Do(t => t.Initialize(fromSave));
            foreach (PatchTreeDef patchTree in patchTrees)
            {
                patchTree.Initialize(fromSave);
                patchTree.Perform();
            }
            /*settings.modList = currMods.ToHashSet();
            settings.patchTrees = patchTrees.ToList();
            settings.Write();*/
        }
    }
}
