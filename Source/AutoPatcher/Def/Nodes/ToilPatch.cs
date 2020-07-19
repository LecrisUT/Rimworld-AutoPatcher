using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using HarmonyLib;

namespace AutoPatcher
{
    // Base ToilPatch
    public class ToilPatch : PatchNode<(Type type, Type ntype, MethodInfo method), (Type type, Type ntype, MethodInfo action)>
    {
        protected static bool successfull = true;
    }
    // ReplaceToil
    /*public class ReplaceToil : PatchNode<(Type type, Type ntype, MethodInfo method), (Type type, Type ntype, MethodInfo ToilGenerator), List<(int pos, MethodInfo action)>>
    {
        protected static bool successfull = true;
        private static List<(int pos, MethodInfo action)> Targets;
        private Dictionary<MethodInfo, MethodInfo> actionToGenerator;
        private static Dictionary<StatDef, FieldInfo> actionToGenerator;
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(ReplaceStat), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetData<(Type type, Type ntype, MethodInfo method)>().ToList();
            var targetCI = InputA(node.inputPorts).GetData<List<(int pos, MethodInfo action)>>().ToList();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Targets = targetCI[i];
                harmony.Patch(TypeMethods[i].method, transpiler: transpiler);
                if (successfull)
                    SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            successfull = false;
            List<CodeInstruction> instuctionList = instructions.ToList();
            foreach ((int pos, StatDef stat) target in Targets)
                if (replaceStatFieldStatic.ContainsKey(target.stat))
                {
                    successfull = true;
                    instuctionList[target.pos].operand = replaceStatFieldStatic[target.stat];
                }
            return instuctionList;
        }
    }*/
    // AP_ToilPatch
    public class AP_ToilPatch : PatchNode<(Type type, Type ntype, MethodInfo method), (Type type, Type ntype, MethodInfo action), List<(int pos, List<MethodInfo> actions)>>
    {
        private Type HelperType;
        private MethodInfo HelperPrepare;
        private MethodInfo HelperTranspile;
        private static MethodInfo HelperTranspileStatic;
        private static List<(int pos, List<MethodInfo> actions)> Targets;
        protected static bool successfull = true;
        public override void Initialize()
        {
            HelperPrepare = AccessTools.Method(HelperType, "Prepare");
            HelperTranspile = AccessTools.Method(HelperType, "Transpile");
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            HelperTranspileStatic = HelperTranspile;
            var thisTranspiler = new HarmonyMethod(AccessTools.Method(typeof(AP_ToilPatch), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetData<(Type type, Type ntype, MethodInfo method)>().ToList();
            var targetCI = InputA(node.inputPorts).GetData<List<(int pos, List<MethodInfo> actions)>>().ToList();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                Targets = targetCI[i];
                List<MethodInfo> actions = Targets.SelectMany(t => t.actions).ToList();
                actions.RemoveDuplicates();
                if (Helper_Prepare(type, ntype, method, actions))
                {
                    harmony.Patch(method, transpiler: thisTranspiler);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            Targets.SortByDescending(t => t.pos);
            successfull = false;
            foreach (var target in Targets)
            {
                if (Helper_Transpile(ref instructionList, target.pos, target.actions))
                    successfull = true;
            }
            if (!successfull)
                return null;
            return instructionList;
        }
        public static bool Helper_Transpile(ref List<CodeInstruction> instructions, int pos, List<MethodInfo> actions)
            => (bool)HelperTranspileStatic.Invoke(null, new object[] { instructions, pos, actions });
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, List<MethodInfo> actions)
            => (bool)HelperPrepare.Invoke(null, new object[] { type, ntype, method, actions });
    }
}
