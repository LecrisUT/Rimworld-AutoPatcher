using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;

namespace AutoPatcher
{
    // Base StatPatch
    public class StatPatch : PatchNode<(Type type, Type ntype, MethodInfo method), StatDef>
    {
        protected static bool successfull = true;
    }
    // ReplaceStat
    public class ReplaceStat : PatchNode<(Type type, Type ntype, MethodInfo method), StatDef, List<(int pos, StatDef stat)>>
    {
        private Dictionary<StatDef, StatDef> replaceStat;
        private Dictionary<StatDef, FieldInfo> replaceStatField;
        private static Dictionary<StatDef, FieldInfo> replaceStatFieldStatic;
        protected static bool successfull = true;
        private static List<(int pos, StatDef stat)> Targets;
        public override void Initialize()
        {
            if (!replaceStatField.EnumerableNullOrEmpty())
                return;
            if (!replaceStat.EnumerableNullOrEmpty())
            {
                List<StatDef> statOut = replaceStat.Values.ToList();
                statOut.RemoveDuplicates();
                replaceStatField = replaceStat.Keys.ToDictionary(t => t, t => null as FieldInfo);
                foreach (Type type in GenTypes.AllTypesWithAttribute<DefOf>())
                    foreach (FieldInfo field in type.GetFields().Where(t => t.FieldType == typeof(StatDef)))
                        if (statOut.FirstOrFallback(t => field.Name.Equals(t.defName)) is StatDef stat && !replaceStatField.ContainsKey(stat))
                            foreach (KeyValuePair<StatDef, StatDef> pair in replaceStat.Where(t => t.Value == stat))
                                replaceStatField[pair.Key] = field;
                return;
            }
            Log.Error($"[[LC]AutoPatcher]: TempError 451356");
        }
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(ReplaceStat), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetData<(Type type, Type ntype, MethodInfo method)>().ToList();
            var targetCI = InputA(node.inputPorts).GetData<List<(int pos, StatDef stat)>>().ToList();
            replaceStatFieldStatic = replaceStatField;
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
    }
    // HarmonyStat
    public class HarmonyStat : StatPatch
    {
        private MethodInfo PrepareMethod;
        private HarmonyMethod Transpiler;
        private HarmonyMethod Prefix;
        private HarmonyMethod Postfix;
        private HarmonyMethod Finalizer;
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var TypeMethods = node.inputPorts[0].GetData<(Type type, Type ntype, MethodInfo method)>().ToList();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                if (Helper_Prepare(type, ntype, method))
                {
                    harmony.Patch(method, Prefix, Postfix, Transpiler, Finalizer);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method)
            => PrepareMethod?.Invoke(null, new object[] { type, ntype, method }).ChangeType<bool>() ?? true;
    }
    // AP_StatPatch
    public class AP_StatPatch : PatchNode<(Type type, Type ntype, MethodInfo method), StatDef, List<(int pos, StatDef stat)>>
    {
        private Type HelperType;
        private MethodInfo HelperPrepare;
        private MethodInfo HelperTranspile;
        private static MethodInfo HelperTranspileStatic;
        private static List<(int pos, StatDef stat)> Targets;
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
            var thisTranspiler = new HarmonyMethod(AccessTools.Method(typeof(AP_StatPatch), "Transpiler"));
            var TypeMethods = node.inputPorts[0].GetData<(Type type, Type ntype, MethodInfo method)>().ToList();
            var targetCI = InputA(node.inputPorts).GetData<List<(int pos, StatDef stat)>>().ToList();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                Targets = targetCI[i];
                List<StatDef> stats = Targets.ConvertAll(t => t.stat);
                if (Helper_Prepare(type, ntype, method, stats))
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
            foreach ((int pos, StatDef stat) target in Targets)
            {
                if (Helper_Transpile(ref instructionList, target.pos, target.stat))
                    successfull = true;
            }
            if (!successfull)
                return null;
            return instructionList;
        }
        public static bool Helper_Transpile(ref List<CodeInstruction> instructions, int pos, StatDef stat)
            => (bool)HelperTranspileStatic.Invoke(null, new object[] { instructions, pos, stat });
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, List<StatDef> stats)
            => (bool)HelperPrepare.Invoke(null, new object[] { type, ntype, method, stats });
    }
}
