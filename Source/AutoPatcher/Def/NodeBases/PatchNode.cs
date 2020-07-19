using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace AutoPatcher
{
    public class PatchNode<InOutT, TargetT> : PassNode<InOutT, InOutT>
    {
        public PatchNode()
        {
            baseInPorts = 2;
            baseOutPorts = 1;
            nOutPortGroups = 2;
            nInPortGroups = 1;
            nOutPorts = nOutPortGroups * baseOutPorts;
            nInPorts = nInPortGroups * baseInPorts;
        }
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
            var data = node.inputPorts[0].GetData<InOutT>().ToList();
            SuccessfulPorts(node)[0].GetData<InOutT>().Do(t => data.Remove(t));
            FailedPorts(node)[0].SetData(data);
            return true;
        }
    }
    public class PatchNode<InOutT, TargetT, InputAT> : PatchNode<InOutT, TargetT>
    {
        public PatchNode()
        {
            baseInPorts = 3;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputAT>());
        }
        public IPort InputA(List<IPort> ports) => ports[2];
    }
    public class PatchNode<InOutT, TargetT, InputAT, InputBT> : PatchNode<InOutT, TargetT, InputAT>
    {
        public PatchNode()
        {
            baseInPorts = 4;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputBT>());
        }
        public IPort InputB(List<IPort> ports) => ports[3];
    }
    public class PatchNode<InOutT, TargetT, InputAT, InputBT, InputCT> : PatchNode<InOutT, TargetT, InputAT, InputBT>
    {
        public PatchNode()
        {
            baseInPorts = 5;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputCT>());
        }
        public IPort InputC(List<IPort> ports) => ports[4];
    }
    public class PatchNode<InOutT, TargetT, InputAT, InputBT, InputCT, InputDT> : PatchNode<InOutT, TargetT, InputAT, InputBT, InputCT>
    {
        public PatchNode()
        {
            baseInPorts = 6;
            nInPorts = nInPortGroups * baseInPorts;
        }
        protected override void CreateInputPortGroup(Node node, int group)
        {
            base.CreateInputPortGroup(node, group);
            node.inputPorts.Add(new Port<InputDT>());
        }
        public IPort InputD(List<IPort> ports) => ports[5];
    }
}
