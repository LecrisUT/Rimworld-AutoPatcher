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
    // HarmonyStat
    public class HarmonyToil : PatchNode<(Type type, Type ntype, MethodInfo method), (Type type, Type ntype, MethodInfo action), List<(int pos, int ToilIndex, List<MethodInfo> actions)>>
    {
        private MethodInfo PrepareMethod;
        private HarmonyMethod Transpiler;
        private HarmonyMethod Prefix;
        private HarmonyMethod Postfix;
        private HarmonyMethod Finalizer;
        private Type PatcherType;
        private string PrepareMethodName;
        private string TranspilerName;
        private string PrefixName;
        private string PostfixName;
        private string FinalizerName;
        public static bool successfull = true;
        public override void Initialize()
        {
            base.Initialize();
            PrepareMethod = PatcherType == null ? AccessTools.Method(PrepareMethodName) : AccessTools.Method(PatcherType, PrepareMethodName ?? "Prepare");
            var method = PatcherType == null ? AccessTools.Method(TranspilerName) : AccessTools.Method(PatcherType, TranspilerName ?? "Transpiler");
            if (method != null)
                Transpiler = new HarmonyMethod(method);
            method = PatcherType == null ? AccessTools.Method(PrefixName) : AccessTools.Method(PatcherType, PrefixName ?? "Prefix");
            if (method != null)
                Prefix = new HarmonyMethod(method);
            method = PatcherType == null ? AccessTools.Method(PostfixName) : AccessTools.Method(PatcherType, PostfixName ?? "Postfix");
            if (method != null)
                Postfix = new HarmonyMethod(method);
            method = PatcherType == null ? AccessTools.Method(FinalizerName) : AccessTools.Method(PatcherType, FinalizerName ?? "Finalizer");
            if (method != null)
                Finalizer = new HarmonyMethod(method);
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var TypeMethods = node.inputPorts[0].GetData<(Type type, Type ntype, MethodInfo method)>().ToList();
            var PositionsList = InputA(node.inputPorts).GetData<List<(int pos, int ToilIndex, List<MethodInfo> actions)>>().ToList();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                method = AccessTools.Method(type, "MakeNewToils");
                var positions = PositionsList[i];
                if (PrepareMethod == null || Helper_Prepare(type, ntype, method, positions))
                {
                    harmony.Patch(method, Prefix, Postfix, Transpiler, Finalizer);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, List<(int pos, int ToilIndex, List<MethodInfo> actions)> positions)
            => PrepareMethod?.Invoke(null, new object[] { type, ntype, method, positions }).ChangeType<bool>() ?? true;
    }
    // AP_ToilPatch
    public class AP_ToilPatch : PatchNode<(Type type, Type ntype, MethodInfo method), (Type type, Type ntype, MethodInfo action), List<(int pos, int ToilIndex, List<MethodInfo> actions)>>
    {
        private Type HelperType;
        private MethodInfo HelperPrepare;
        private MethodInfo HelperTranspile;
        private static MethodInfo HelperTranspileStatic;
        private static List<(int pos, int ToilIndex, List<MethodInfo> actions)> Targets;
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
            var targetCI = InputA(node.inputPorts).GetData<List<(int pos, int ToilIndex, List<MethodInfo> actions)>>().ToList();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                Targets = targetCI[i];
                List<MethodInfo> actions = Targets.SelectMany(t => t.actions).ToList();
                actions.RemoveDuplicates();
                if (HelperPrepare == null || Helper_Prepare(type, ntype, method, actions))
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
