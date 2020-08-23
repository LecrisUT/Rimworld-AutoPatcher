﻿using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using System;
using System.Reflection;

namespace AutoPatcher
{
    public class PatchNode<TargetT, TargetTPos> : PassNode<(Type type, Type ntype, MethodInfo method), (Type type, Type ntype, MethodInfo method)>
    {
        protected override int baseInPorts => 3;
        protected override int nOutPortGroups => 2;
        public Harmony harmony = MainMod.harmony;
        private string HarmonyName;

        public List<IPort> SuccessfulPorts(Node node)
            => node.outputPorts.GetRange(0, baseOutPorts);
        public List<IPort> FailedPorts(Node node)
            => node.outputPorts.GetRange(baseOutPorts, baseOutPorts);
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<TargetT>());
            node.inputPorts.Add(new Port<List<TargetTPos>>());
        }
        public override bool Initialize(Node node)
        {
            if (!base.Initialize(node))
                return false;
            if (HarmonyName != null)
                harmony = new Harmony(HarmonyName);
            return true;
        }
        public override bool PostPerform(Node node)
        {
            if (!base.PostPerform(node))
                return false;
            var data = node.inputPorts[0].GetDataList<(Type type, Type ntype, MethodInfo method)>();
            SuccessfulPorts(node)[0].GetData<(Type type, Type ntype, MethodInfo method)>().Do(t => data.Remove(t));
            FailedPorts(node)[0].SetData(data);
            return true;
        }
        public IPort Target(List<IPort> ports) => ports[1];
        public IPort TargetPos(List<IPort> ports) => ports[2];
    }
}
