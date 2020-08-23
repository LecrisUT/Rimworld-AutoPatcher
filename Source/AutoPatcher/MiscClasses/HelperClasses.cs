using AutoPatcher.Utility;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AutoPatcher
{
    public class LocalVar
    {
        public readonly MethodInfo method;
        public readonly int index = -1;
        public readonly LocalBuilder builder;
        public LocalVar(MethodInfo m, CodeInstruction instruction)
        {
            method = m;
            if (!instruction.IsLdloc() && !instruction.IsStloc())
                throw new Exception("Code instruction is not a Ldloc or Stloc");
            var t = instruction.ToLocalVar();
            index = t.index;
            builder = t.builder;
        }
        public LocalVar(LocalBuilder b)
        {
            builder = b;
        }
        public LocalVar(int i)
        {
            index = i;
        }
        public static readonly Dictionary<OpCode, int> OpCodeToInt = new Dictionary<OpCode, int>
        {
            { OpCodes.Ldloc_0, 0 },
            { OpCodes.Ldloc_1, 1 },
            { OpCodes.Ldloc_2, 2 },
            { OpCodes.Ldloc_3, 3 },
            { OpCodes.Stloc_0, 0 },
            { OpCodes.Stloc_1, 1 },
            { OpCodes.Stloc_2, 2 },
            { OpCodes.Stloc_3, 3 },
        };
        public static implicit operator LocalBuilder(LocalVar local) => local.builder;
        public static implicit operator int(LocalVar local) => local.index;
        public static explicit operator LocalVar(LocalBuilder local) => local == null ? null : new LocalVar(local);
        public static explicit operator LocalVar(int local) => local < 0 ? null : local > 3 ? null : new LocalVar(local);
    }
    public class TypeMethod
    {
        public Type type;
        public Type ntype;
        public MethodInfo method;
        public TypeMethod() { }
        public TypeMethod(MethodInfo m)
        {
            method = m;
            ntype = method.DeclaringType;
            type = ntype;
            while (type.DeclaringType != null)
                type = type.DeclaringType;
            if (ntype == type)
                ntype = null;
        }
        public TypeMethod(Type t, Type nt, MethodInfo m)
        {
            type = t;
            ntype = nt;
            method = m;
        }
        public static implicit operator (Type, Type, MethodInfo)(TypeMethod tm) => (tm.type, tm.ntype, tm.method);
        public static explicit operator TypeMethod((Type, Type, MethodInfo) tm) => new TypeMethod(tm.Item1, tm.Item2, tm.Item3);
        public static explicit operator TypeMethod(MethodInfo method) => new TypeMethod(method);
        public override string ToString()
            => $"{type} : {ntype} : {method}";
    }
    public class EnumInfo
    {
        public FieldInfo Current;
        public FieldInfo State;
        public LocalVar localState;
        public EnumInfo() { }
        public EnumInfo(FieldInfo cur, FieldInfo sta, LocalVar localSta = null)
        {
            Current = cur;
            State = sta;
            localState = localSta;
        }
        public static implicit operator (FieldInfo, FieldInfo)(EnumInfo info) => (info.Current, info.State);
        public static implicit operator (FieldInfo, FieldInfo, LocalVar)(EnumInfo info) => (info.Current, info.State, info.localState);
        public static explicit operator EnumInfo((FieldInfo, FieldInfo) info) => new EnumInfo(info.Item1, info.Item2);
        public static explicit operator EnumInfo((FieldInfo, FieldInfo, LocalVar) info) => new EnumInfo(info.Item1, info.Item2, info.Item3);
    }
    public class EnumItemInfo
    {
        public Label label;
        public int startPos;
        public int endPos;
        public EnumItemInfo() { }
        public EnumItemInfo(Label lab, int start, int end)
        {
            label = lab;
            startPos = start;
            endPos = end;
        }
        public static implicit operator (Label, int, int)(EnumItemInfo info) => (info.label, info.startPos, info.endPos);
        public static explicit operator EnumItemInfo((Label, int, int) info) => new EnumItemInfo(info.Item1, info.Item2, info.Item3);
    }
}
