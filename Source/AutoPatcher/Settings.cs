using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AutoPatcher
{
    public class Settings : ModSettings
    {
        public List<PatchTreeDef> patchTrees;
        public HashSet<string> modList;
        public void DoWindowContents(Rect wrect)
        {
            Listing_Standard options = new Listing_Standard();
            Color defaultColor = GUI.color;
            options.Begin(wrect);

            GUI.color = defaultColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            options.Gap();
            /*bool reset = options.ButtonText("Reset the saved data");
            if (reset)
            {
                modList = ModsConfig.ActiveModsInLoadOrder.Select(t => t.PackageId).ToHashSet();
                patchTrees = null;
            }*/
            options.End();
            //Mod.GetSettings<Settings>().Write();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref patchTrees, "patchTrees", LookMode.Deep);
            Scribe_Collections.Look(ref modList, "modList");
        }
    }
}
