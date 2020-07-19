using HarmonyLib;
using Verse;

namespace AutoPatcher
{
    public sealed class MainMod : Mod
    {
        public static ModContentPack ourContentPack;
        public static Harmony harmony;
        public MainMod(ModContentPack content) : base(content)
        {
            // Early patch to avoid generic Def issues
            harmony = new Harmony("AutoPatcher");
            harmony.Patch(AccessTools.Method(typeof(GenTypes), "AllSubclasses"),
                postfix: new HarmonyMethod(typeof(Patch_GenTypes_AllSubclasses), "Postfix"));
        }
    }
}
