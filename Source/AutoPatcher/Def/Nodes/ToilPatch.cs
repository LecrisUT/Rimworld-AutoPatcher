using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using AutoPatcher.Utility;
using Verse.AI;

namespace AutoPatcher
{
    // HarmonyStat
    public class HarmonyToil : EnumerableMethodPatchNode<List<MethodInfo>, List<(Label label, int ToilStart, int ToilEnd)>>
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
        public override void Initialize(bool fromSave = false)
        {
            base.Initialize(fromSave);
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
            var TypeMethods = node.inputPorts[0].GetDataList<(Type type, Type ntype, MethodInfo method)>();
            var targetCI = TargetPos(node.inputPorts).GetDataList<List<(int pos, int ToilIndex, List<MethodInfo> actions)>>();
            var toilInfoList = InputA(node.inputPorts).GetDataList<List<(Label label, int ToilStart, int ToilEnd)>>();
            var enumInfoList = EnumInfo(node.inputPorts).GetDataList<(FieldInfo Current, FieldInfo switchField, LocalVar local)>();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                method = AccessTools.Method(type, "MakeNewToils");
                var positions = targetCI[i];
                var actions = positions.SelectMany(t => t.actions).ToList();
                var toilInfo = toilInfoList[i];
                var enumInfo = enumInfoList[i];
                actions.RemoveDuplicates();
                if (Helper_Prepare(type, ntype, method, enumInfo, actions, toilInfo))
                {
                    harmony.Patch(method, Prefix, Postfix, Transpiler, Finalizer);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, (FieldInfo getCurrent, FieldInfo switchField, LocalVar local) enumInfo, List<MethodInfo> actions, List<(Label label, int ToilStart, int ToilEnd)> toilInfo)
        {
            if (PrepareMethod == null)
                return true;
            var param = PrepareMethod.GetParameters();
            var inp3 = param[3];
            var inp5 = param[5];
            if (!enumInfo.TryCastTo(inp3.ParameterType, out var cenumInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 468735468: {PrepareMethod.DeclaringType} : {PrepareMethod}\n" +
                    $"{enumInfo.GetType()} -> {inp3.ParameterType}");
                return false;
            }
            if (!toilInfo.TryCastTo<List<(Label, int, int)>>(inp5.ParameterType, out var ctoilInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 879466879: {PrepareMethod.DeclaringType} : {PrepareMethod}\n" +
                    $"{toilInfo.GetType()} -> {inp5.ParameterType}");
                return false;
            }
            var res = (bool)PrepareMethod.Invoke(null, new object[] { type, ntype, method, cenumInfo, actions, ctoilInfo });
            return res;
        }
    }
    // AP_ToilPatch
    public class AP_ToilPatch : EnumerableMethodPatchNode<List<MethodInfo>, List<(Label label, int ToilStart, int ToilEnd)>>
    {
        private Type HelperType;
        private MethodInfo HelperPrepare;
        private MethodInfo HelperTranspile;
        private static MethodInfo HelperTranspileStatic;
        private static List<(int pos, int ToilIndex, List<MethodInfo> actions)> Targets;
        protected static bool successfull = true;
        public override void Initialize(bool fromSave = false)
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
            var TypeMethods = node.inputPorts[0].GetDataList<(Type type, Type ntype, MethodInfo method)>();
            var targetCI = TargetPos(node.inputPorts).GetDataList<List<(int pos, int ToilIndex, List<MethodInfo> actions)>>();
            var toilInfoList = InputA(node.inputPorts).GetDataList<List<(Label label, int ToilStart, int ToilEnd)>>();
            var enumInfoList = EnumInfo(node.inputPorts).GetDataList<(FieldInfo Current, FieldInfo switchField, LocalVar local) >();
            for (int i = 0; i < TypeMethods.Count; i++)
            {
                Type type = TypeMethods[i].type;
                Type ntype = TypeMethods[i].ntype;
                MethodInfo method = TypeMethods[i].method;
                Targets = targetCI[i];
                List<MethodInfo> actions = Targets.SelectMany(t => t.actions).ToList();
                var toilInfo = toilInfoList[i];
                var enumInfo = enumInfoList[i];
                switchField = enumInfo.switchField;
                actions.RemoveDuplicates();
                if (Helper_Prepare(type, ntype, method, enumInfo, actions, toilInfo))
                {
                    currMethod = method;
                    harmony.Patch(method, transpiler: thisTranspiler);
                    if (successfull)
                        SuccessfulPorts(node)[0].AddData(TypeMethods[i]);
                }
            }
            return true;
        }
        private static MethodInfo currMethod;
        private static List<(int pos, int offset)> Offsets;
        private static List<(Label label, int pos, int length)> NewItems;
        private static FieldInfo switchField;
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            Offsets = new List<(int, int)>();
            NewItems = new List<(Label, int, int)>();
            Targets.SortBy(t => t.pos);
            successfull = false;
            foreach (var target in Targets)
            {
                int pos = target.pos;
                foreach (var offset in Offsets)
                    if (pos >= offset.pos)
                        pos += offset.offset;
                if (Helper_Transpile(ref instructionList, generator, pos, target.actions, out var offsets, out var newItems))
                {
                    successfull = true;
                    if (!offsets.NullOrEmpty())
                        Offsets.AddRange(offsets);
                    if (!newItems.NullOrEmpty())
                        NewItems.AddRange(newItems);
                }
            }
            NewItems.SortBy(t => t.pos);
            NodeUtility.RegisterOffset(currMethod, Offsets);
            NodeUtility.AddEnumerableItem(currMethod, NewItems);
            if (successfull && NodeUtility.enumerableItems.TryGetValue(currMethod, out var items))
            {
                for (int i = 0; i < instructionList.Count; i++)
                    if (instructionList[i].opcode == OpCodes.Switch)
                    {
                        instructionList[i].operand = items.Select(t => t.label).ToArray();
                        break;
                    }
                int offset = 0;
                foreach (var item in items)
                {
                    if (NewItems.Any(t => item.startPos == t.pos))
                        continue;
                    if (NewItems.Any(t => item.startPos > t.pos))
                    {
                        offset++;
                        NewItems.RemoveAll(t => item.startPos > t.pos);
                    }
                    for (int i = item.startPos; i <= item.endPos; i++)
                    {
                        var ins = instructionList[i];
                        var ins2 = instructionList[i - 1];
                        if (ins.StoresField(switchField) && ins2.LoadsConstant())
                        {
                            if (ins2.LoadsConstant(-1))
                                continue;
                            for (int l = 0; l < int.MaxValue; l++)
                            {
                                if (ins2.LoadsConstant(l))
                                {
                                    instructionList[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, l + offset);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (!successfull)
                return null;
            return instructionList;
        }
        public static bool Helper_Transpile(ref List<CodeInstruction> instructions, ILGenerator generator, int pos, List<MethodInfo> actions, out List<(int pos, int offset)> CIOffsets, out List<(Label label, int pos, int length)> newItems)
        {
            var param = new object[] { instructions, generator, pos, actions, null, null };
            var res = (bool)HelperTranspileStatic.Invoke(null, param);
            CIOffsets = (List<(int,int)>)param[4];
            newItems = (List<(Label, int, int)>)param[5];
            return res;
        }
        public bool Helper_Prepare(Type type, Type ntype, MethodInfo method, (FieldInfo getCurrent, FieldInfo switchField, LocalVar local) enumInfo, List<MethodInfo> actions, List<(Label label, int ToilStart, int ToilEnd)> toilInfo)
        {
            if (HelperPrepare == null)
                return true;
            var param = HelperPrepare.GetParameters();
            var inp3 = param[3];
            var inp5 = param[5];
            if (!enumInfo.TryCastTo(inp3.ParameterType, out var cenumInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 468735468: {HelperPrepare.DeclaringType} : {HelperPrepare}\n" +
                    $"{enumInfo.GetType()} -> {inp3.ParameterType}");
                return false;
            }
            if (!toilInfo.TryCastTo<List<(Label,int,int)>>(inp5.ParameterType, out var ctoilInfo))
            {
                Log.Error($"[[LC]AutoPatcher] TempError 879466879: {HelperPrepare.DeclaringType} : {HelperPrepare}\n" +
                    $"{toilInfo.GetType()} -> {inp5.ParameterType}");
                return false;
            }
            var res = (bool)HelperPrepare.Invoke(null, new object[] { type, ntype, method, cenumInfo, actions, ctoilInfo });
            return res;
        }
    }
}
