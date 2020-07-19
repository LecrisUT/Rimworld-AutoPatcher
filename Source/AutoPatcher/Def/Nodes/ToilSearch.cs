using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using HarmonyLib;
using System.Reflection.Emit;
using Verse.AI;
using System.Collections;

namespace AutoPatcher
{
    public class JobDriverSearchToil : SearchNode<Type, (Type type, Type ntype, MethodInfo action), (Type type, Type ntype, MethodInfo method), List<(int pos, List<MethodInfo> actions)>>
    {
        public override bool Perform(Node node)
        {
            if (!base.Perform(node))
                return false;
            var input = node.inputPorts[0].GetData<Type>();
            var targets = node.inputPorts[1].GetData<(Type type, Type ntype, MethodInfo action)>().ToList();
            var foundPorts = FoundPorts(node);
            var targetMethods = targets.ConvertAll(t => t.action);
            var ToilGenerators = new List<(MethodInfo generator, List<MethodInfo> actions)>();
            // Search for Toil generators
            foreach (Type type in input)
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type).Where(t => t.ReturnType == typeof(Toil)))
                    if (SearchToilGenerator(method, targetMethods, out List<MethodInfo> ActionsFound))
                        ToilGenerators.Add((method, ActionsFound));
            // Search for Toils in MakeNewToils
            foreach (Type type in input)
            {
                var MakeNewToils = AccessTools.Method(type, "MakeNewToils");
                foreach (Type nType in type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (nType.GetInterfaces()?.Any(t => t == typeof(IEnumerator<Toil>)) == true)
                    {
                        var interfaceMap = nType.GetInterfaceMap(typeof(IEnumerator));
                        var MoveNext = AccessTools.Method(nType, "MoveNext");
                        var get_Current = nType.GetInterfaceMap(typeof(IEnumerator<Toil>)).TargetMethods.First();
                        var Current = GetFieldInfo(get_Current);
                        if (SearchMoveNext(MoveNext, Current, targetMethods, ToilGenerators, out List<(int pos, List<MethodInfo> actions)> SearchResults))
                        {
                            foundPorts[0].AddData(type);
                            ResultA(foundPorts).AddData((type, nType, MoveNext));
                            ResultB(foundPorts).AddData(SearchResults);
                        }
                        break;
                    }
                }
            }
            var foundActions = ResultB(foundPorts).GetData<List<(int pos, List<MethodInfo> actions)>> ().SelectMany(t => t.SelectMany(tt => tt.actions)).ToList();
            foundActions.RemoveDuplicates();
            foundPorts[1].SetData(targets.Where(t=>foundActions.Contains(t.action)).ToList());
            return true;
        }
        private static bool IsBaseMethod(MethodInfo finalMethod, MethodInfo baseMethod)
        {
            MethodInfo currMethod = finalMethod;
            MethodInfo prevMethod = null;
            while (currMethod != prevMethod)
            {
                if (currMethod == baseMethod)
                    return true;
                prevMethod = currMethod;
                currMethod = currMethod.GetBaseDefinition();
            }
            return false;
        }
        private static FieldInfo GetFieldInfo(MethodInfo getterMethod)
        {
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(getterMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(getterMethod); }
            var length = instructionList.Count;
            for (int i = 0; i < length; i++)
            {
                CodeInstruction instruction = instructionList[length - 1 - i];
                if (instruction.opcode == OpCodes.Ldfld && instructionList[length - 2 - i].IsLdarg(0))
                    return instruction.operand as FieldInfo;
            }
            return null;
        }
        private bool SearchToilGenerator(MethodInfo searchMethod, List<MethodInfo> ActionList, out List<MethodInfo> ActionsFound)
        {
            ActionsFound = new List<MethodInfo>();
            bool foundResult = false;
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            foreach (var instruction in instructionList)
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                    instruction.operand is MethodInfo calledMethod && ActionList.Any(t => IsBaseMethod(t,calledMethod)))
                {
                    foundResult = true;
                    ActionsFound.Add(calledMethod);
                    if (!FindAll)
                        return true;
                }
            return foundResult;
        }
        public bool SearchMoveNext(MethodInfo searchMethod, FieldInfo Current, List<MethodInfo> ActionList, List<(MethodInfo generator, List<MethodInfo> actions)> ToilGenerators, out List<(int pos, List<MethodInfo> actions)> Results)
        {
            Results = new List<(int, List<MethodInfo>)>();
            var ActionsFound = new List<MethodInfo>();
            bool foundResult = false;
            bool flagFound = false;
            var Action_ctor = AccessTools.Constructor(typeof(Action), new[] { typeof(object), typeof(IntPtr) });
            List<CodeInstruction> instructionList;
            try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.Is(OpCodes.Newobj, Action_ctor))
                {
                    var prevInstruction = instructionList[i - 1];
                    if (prevInstruction.opcode == OpCodes.Ldftn && prevInstruction.operand is MethodInfo method && 
                        ActionList.Contains(method))
                    {
                        flagFound = true;
                        ActionsFound.Add(method);
                    }
                }
                if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                    instruction.operand is MethodInfo calledMethod && ToilGenerators.Any(t => t.generator == calledMethod))
                {
                    flagFound = true;
                    ActionsFound.AddRange(ToilGenerators.First(t => t.generator == calledMethod).actions);
                }
                if (flagFound && instruction.StoresField(Current))
                {
                    foundResult = true;
                    Results.Add((i, ActionsFound));
                    if (!FindAll)
                        return true;
                    ActionsFound = new List<MethodInfo>();
                    flagFound = false;
                }
            }
            return foundResult;
        }
    }
}
