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
    public class JobDriverSearchToil : SearchNode<Type, (Type type, Type ntype, MethodInfo action), (Type type, Type ntype, MethodInfo method), List<(int pos, int ToilIndex, List<MethodInfo> actions)>>
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
                        if (SearchMoveNext(MoveNext, Current, targetMethods, ToilGenerators, out List<(int pos, int ToilIndex, List<MethodInfo> actions)> SearchResults))
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
        public bool SearchMoveNext(MethodInfo searchMethod, FieldInfo Current, List<MethodInfo> ActionList, List<(MethodInfo generator, List<MethodInfo> actions)> ToilGenerators, out List<(int pos, int ToilIndex, List<MethodInfo> actions)> Results)
        {
            var test = new System.Text.StringBuilder($"TEst 0: {searchMethod}\n");
            var test1 = new System.Text.StringBuilder($"TEst 1: {searchMethod}\n");
            Results = new List<(int, int, List<MethodInfo>)>();
            var ActionsFound = new List<MethodInfo>();
            bool foundResult = false;
            bool flagFound = false;
            int ToilIndex = 0;
            bool foundDefault = false;
            Label? retLabel = null;
            Label[] otherLabels = new Label[0];
            var Action_ctor = AccessTools.Constructor(typeof(Action), new[] { typeof(object), typeof(IntPtr) });
            List<CodeInstruction> instructionList;
            // try { instructionList = PatchProcessor.GetCurrentInstructions(searchMethod); }
            // catch { instructionList = PatchProcessor.GetOriginalInstructions(searchMethod); }
            instructionList = PatchProcessor.GetCurrentInstructions(searchMethod);
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];
                test1.AppendLine($"[{i}:{instruction.labels.Count}] {instruction.opcode} : [{instruction.operand?.GetType()}] {instruction.operand} :: [{instruction.opcode == OpCodes.Switch}]");
            }
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
                    Results.Add((i, ToilIndex, ActionsFound));
                    test.AppendLine($"Found Toil: [{i}] {ToilIndex}");
                    if (!FindAll)
                        return true;
                    ActionsFound = new List<MethodInfo>();
                    flagFound = false;
                }
                if (instruction.opcode == OpCodes.Switch)
                {
                    test.AppendLine($"Found Switch [{i}]");
                    otherLabels = (Label[])instruction.operand;
                    foreach (var tlabel in otherLabels)
                    {
                        for (int k = 0; k < instructionList.Count; k++)
                        {
                            var tInstruction = instructionList[k];
                            if (tInstruction.labels.Contains(tlabel))
                                test.AppendLine($"tlabel = {tlabel} [{k}]");
                        }
                    }
                    for (int j = 1; j < instructionList.Count - i; j++)
                    {
                        var nextInstruction = instructionList[i + j];
                        test.AppendLine($"[j={j}] {nextInstruction.opcode} : {nextInstruction.operand} :: {nextInstruction.opcode == OpCodes.Ret}");
                        if (nextInstruction.opcode == OpCodes.Ret)
                        {
                            test.AppendLine($"Found default [{i + j}]");
                            foundDefault = true;
                            i += j;
                            break;
                        }
                        if (nextInstruction.Branches(out var labelSwitch))
                        {
                            retLabel = labelSwitch;
                            test.AppendLine($"Found default [{i + j}]: {retLabel.HasValue}");
                            if (retLabel.HasValue)
                            {
                                for (int k = 0; k < instructionList.Count; k++)
                                {
                                    var tInstruction = instructionList[k];
                                    if (tInstruction.labels.Contains(retLabel.Value))
                                        test.AppendLine($"retLabel = {retLabel.Value} [{k}]");
                                }
                            }
                            foundDefault = true;
                            i += j;
                            break;
                        }
                    }
                }
                if (instruction.Branches(out var label2))
                {
                    test.AppendLine($"Branches: [{i}]: {label2} [{label2.HasValue}]: {label2 == retLabel}");
                    if (label2.HasValue && retLabel.HasValue)
                        for (int k = 0; k < instructionList.Count; k++)
                        {
                            var tInstruction = instructionList[k];
                            if (tInstruction.labels.Contains(label2.Value))
                                test.AppendLine($"Label2 = {label2.Value} [{k}]");
                        }
                }

                // if (foundDefault && instruction.opcode == OpCodes.Ret)
                if (foundDefault && (instruction.opcode == OpCodes.Ret || (retLabel.HasValue && instruction.Branches(out var label) && label.Value == retLabel.Value)))
                {
                    ToilIndex++;
                    test.AppendLine($"Inceased Index: [{i}] {ToilIndex}");
                }
            }
            if (foundResult)
            {
                // Log.Message(test.ToString());
                // Log.Message(test1.ToString());
            }
            return foundResult;
        }
    }
}
