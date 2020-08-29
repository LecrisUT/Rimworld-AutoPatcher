using HarmonyLib;
using UnityEngine;
using Verse;

namespace AutoPatcher
{
    public sealed class MainMod : Mod
    {
        public Settings settings;
        public static ModContentPack ourContentPack;
        public static Harmony harmony;
        public static MainMod thisMod;
        public MainMod(ModContentPack content) : base(content)
        {
            thisMod = this;
            // Early patch to avoid generic Def issues
            harmony = new Harmony("AutoPatcher");
            harmony.Patch(AccessTools.Method(typeof(GenTypes), "AllSubclasses"),
                postfix: new HarmonyMethod(typeof(Patch_GenTypes_AllSubclasses), "Postfix"));
            settings = GetSettings<Settings>();
        }
        public override string SettingsCategory() => "AutoPatcher".Translate();
        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
