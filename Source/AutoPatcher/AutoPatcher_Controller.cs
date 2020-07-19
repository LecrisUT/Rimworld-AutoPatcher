using AutoPatcher.Utility;
using HarmonyLib;
using HugsLib;
using System.Collections.Generic;
using Verse;

namespace AutoPatcher
{
    internal class Controller : ModBase
    {
        public override void DefsLoaded()
        {
            // Run patch trees
            List<PatchTreeDef> patchTrees = DefDatabase<PatchTreeDef>.AllDefsListForReading;
            NodeUtility.allNodeDefs.Do(t => t.Initialize());
            foreach (PatchTreeDef patchTree in patchTrees)
            {
                patchTree.Initialize();
                patchTree.Perform();
            }
        }
    }
}
