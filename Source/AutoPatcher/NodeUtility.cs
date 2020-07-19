using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace AutoPatcher.Utility
{
    /// <summary>
    /// Helper functions
    /// </summary>
    public static class NodeUtility
    {
        public static List<NodeDef> allNodeDefs = new List<NodeDef>();
        private static Type thisType = typeof(NodeUtility);
        public static MethodInfo getDefListInfo = AccessTools.Method(thisType, "GetDefList");
        public static List<Def> GetDefList<T>() where T : Def
            => DefDatabase<T>.AllDefsListForReading.ConvertAll(t => (Def)t);
    }
}
